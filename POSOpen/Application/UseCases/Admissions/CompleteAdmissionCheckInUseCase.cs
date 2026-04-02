using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Application.Security;
using POSOpen.Application.UseCases.Sync;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Admissions;

public sealed class CompleteAdmissionCheckInUseCase
{
	private readonly EvaluateFastPathCheckInUseCase _evaluateFastPathCheckInUseCase;
	private readonly ProfileAdmissionUseCase _profileAdmissionUseCase;
	private readonly ICurrentSessionService _currentSessionService;
	private readonly IAuthorizationPolicyService _authorizationPolicyService;
	private readonly IAdmissionSettlementService _admissionSettlementService;
	private readonly IAdmissionCheckInRepository _admissionCheckInRepository;
	private readonly IOfflineActionQueueService _offlineActionQueueService;
	private readonly IOperationContextFactory _operationContextFactory;
	private readonly IAppStateService _appStateService;
	private readonly ILogger<CompleteAdmissionCheckInUseCase> _logger;

	public CompleteAdmissionCheckInUseCase(
		EvaluateFastPathCheckInUseCase evaluateFastPathCheckInUseCase,
		ProfileAdmissionUseCase profileAdmissionUseCase,
		ICurrentSessionService currentSessionService,
		IAuthorizationPolicyService authorizationPolicyService,
		IAdmissionSettlementService admissionSettlementService,
		IAdmissionCheckInRepository admissionCheckInRepository,
		IOfflineActionQueueService offlineActionQueueService,
		IOperationContextFactory operationContextFactory,
		IAppStateService appStateService,
		ILogger<CompleteAdmissionCheckInUseCase> logger)
	{
		_evaluateFastPathCheckInUseCase = evaluateFastPathCheckInUseCase;
		_profileAdmissionUseCase = profileAdmissionUseCase;
		_currentSessionService = currentSessionService;
		_authorizationPolicyService = authorizationPolicyService;
		_admissionSettlementService = admissionSettlementService;
		_admissionCheckInRepository = admissionCheckInRepository;
		_offlineActionQueueService = offlineActionQueueService;
		_operationContextFactory = operationContextFactory;
		_appStateService = appStateService;
		_logger = logger;
	}

	public async Task<AppResult<AdmissionCompletionResultDto>> ExecuteAsync(
		CompleteAdmissionCheckInCommand command,
		CancellationToken ct = default)
	{
		if (command.AmountCents <= 0 || string.IsNullOrWhiteSpace(command.CurrencyCode))
		{
			return AppResult<AdmissionCompletionResultDto>.Failure(
				CompleteAdmissionCheckInConstants.ErrorAmountRequired,
				CompleteAdmissionCheckInConstants.SafeAmountRequiredMessage);
		}

		var session = _currentSessionService.GetCurrent();
		if (session is null || !_authorizationPolicyService.HasPermission(session.Role, RolePermissions.AdmissionsLookup))
		{
			return AppResult<AdmissionCompletionResultDto>.Failure(
				CompleteAdmissionCheckInConstants.ErrorAuthForbidden,
				CompleteAdmissionCheckInConstants.SafeAuthForbiddenMessage);
		}

		var fastPathResult = await _evaluateFastPathCheckInUseCase.ExecuteAsync(
			new EvaluateFastPathCheckInQuery(command.FamilyId, true),
			ct);

		if (!fastPathResult.IsSuccess || fastPathResult.Payload is null || !fastPathResult.Payload.IsEligible)
		{
			return AppResult<AdmissionCompletionResultDto>.Failure(
				CompleteAdmissionCheckInConstants.ErrorFastPathBlocked,
				CompleteAdmissionCheckInConstants.SafeFastPathBlockedMessage);
		}

		var profileResult = await _profileAdmissionUseCase.InitializeAsync(
			new InitializeProfileAdmissionDraftQuery(command.FamilyId, null),
			ct);

		if (!profileResult.IsSuccess || profileResult.Payload is null || profileResult.Payload.MissingRequiredFields.Count > 0)
		{
			return AppResult<AdmissionCompletionResultDto>.Failure(
				CompleteAdmissionCheckInConstants.ErrorFastPathBlocked,
				CompleteAdmissionCheckInConstants.SafeFastPathBlockedMessage);
		}

		var operationContext = _operationContextFactory.CreateRoot();
		var settlementDecision = await _admissionSettlementService.AttemptAuthorizationAsync(
			command.FamilyId,
			command.AmountCents,
			command.CurrencyCode,
			operationContext,
			ct);

		if (settlementDecision.DecisionType == AdmissionSettlementDecisionType.NonEligibleFailure)
		{
			return AppResult<AdmissionCompletionResultDto>.Failure(
				CompleteAdmissionCheckInConstants.ErrorCompletionFailed,
				settlementDecision.FailureMessage ?? CompleteAdmissionCheckInConstants.SafeCompletionFailedMessage,
				settlementDecision.FailureCode);
		}

		var settlementStatus = settlementDecision.DecisionType == AdmissionSettlementDecisionType.Authorized
			? AdmissionSettlementStatus.Authorized
			: AdmissionSettlementStatus.DeferredQueued;

		var confirmationCode = $"CHK-{operationContext.OperationId:N}"[..12].ToUpperInvariant();
		var receiptReference = $"ADM-{operationContext.OccurredUtc:yyyyMMddHHmmss}-{operationContext.OperationId:N}"[..24].ToUpperInvariant();

		var record = AdmissionCheckInRecord.Create(
			Guid.NewGuid(),
			command.FamilyId,
			operationContext.OperationId,
			settlementStatus,
			command.AmountCents,
			command.CurrencyCode.Trim().ToUpperInvariant(),
			operationContext.OccurredUtc,
			confirmationCode,
			receiptReference);

		var operationPayload = new
		{
			familyId = command.FamilyId,
			amountCents = command.AmountCents,
			currencyCode = record.CurrencyCode,
			settlementStatus = settlementStatus.ToString(),
			confirmationCode,
			receiptReference,
			processorReference = settlementDecision.ProcessorReference,
			operationId = operationContext.OperationId,
			occurredUtc = operationContext.OccurredUtc
		};

		var outboxPayload = settlementStatus == AdmissionSettlementStatus.DeferredQueued
			? new
			{
				familyId = command.FamilyId,
				amountCents = command.AmountCents,
				currencyCode = record.CurrencyCode,
				actorStaffId = session.StaffId,
				confirmationCode,
				receiptReference,
				operationId = operationContext.OperationId,
				correlationId = operationContext.CorrelationId,
				occurredUtc = operationContext.OccurredUtc
			}
			: null;

		try
		{
			await _admissionCheckInRepository.SaveCompletionAsync(
				new AdmissionCheckInPersistenceRequest(
					record,
					settlementStatus == AdmissionSettlementStatus.Authorized
						? CompleteAdmissionCheckInConstants.EventAdmissionCompleted
						: CompleteAdmissionCheckInConstants.EventAdmissionPaymentQueued,
					operationPayload,
					operationContext),
				ct);

			if (outboxPayload is not null)
			{
				await _offlineActionQueueService.QueueAsync(
					new QueueOfflineActionCommand(
						CompleteAdmissionCheckInConstants.EventAdmissionPaymentQueued,
						command.FamilyId.ToString(),
						session.StaffId,
						outboxPayload,
						operationContext),
					ct);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(
				ex,
				"Admission completion persistence failed for family {FamilyId} and operation {OperationId}.",
				command.FamilyId,
				operationContext.OperationId);

			var errorCode = settlementStatus == AdmissionSettlementStatus.DeferredQueued
				? CompleteAdmissionCheckInConstants.ErrorQueuePersistenceFailed
				: CompleteAdmissionCheckInConstants.ErrorCompletionFailed;
			var safeMessage = settlementStatus == AdmissionSettlementStatus.DeferredQueued
				? CompleteAdmissionCheckInConstants.SafeQueuePersistenceFailedMessage
				: CompleteAdmissionCheckInConstants.SafeCompletionFailedMessage;

			return AppResult<AdmissionCompletionResultDto>.Failure(errorCode, safeMessage, ex.Message);
		}

		if (settlementStatus == AdmissionSettlementStatus.DeferredQueued)
		{
			_appStateService.SetSyncState("Payment queued for deferred settlement");
		}
		else
		{
			_appStateService.SetSyncState("Admission settled and completed");
		}

		var dto = new AdmissionCompletionResultDto(
			command.FamilyId,
			operationContext.OperationId,
			settlementStatus,
			settlementStatus == AdmissionSettlementStatus.Authorized
				? CompleteAdmissionCheckInConstants.SettlementLabelAuthorized
				: CompleteAdmissionCheckInConstants.SettlementLabelDeferred,
			confirmationCode,
			receiptReference,
			settlementStatus == AdmissionSettlementStatus.Authorized
				? CompleteAdmissionCheckInConstants.AuthorizedGuidance
				: CompleteAdmissionCheckInConstants.DeferredGuidance,
			command.AmountCents,
			record.CurrencyCode,
			record.CompletedAtUtc);

		return AppResult<AdmissionCompletionResultDto>.Success(
			dto,
			settlementStatus == AdmissionSettlementStatus.Authorized
				? CompleteAdmissionCheckInConstants.SuccessAuthorizedMessage
				: CompleteAdmissionCheckInConstants.SuccessDeferredMessage);
	}
}
