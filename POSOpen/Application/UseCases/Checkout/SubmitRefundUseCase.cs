using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Application.Security;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Shared.Operational;

namespace POSOpen.Application.UseCases.Checkout;

public sealed class SubmitRefundUseCase
{
	private readonly ICartSessionRepository _cartSessionRepository;
	private readonly ICheckoutPaymentAttemptRepository _checkoutPaymentAttemptRepository;
	private readonly IRefundRepository _refundRepository;
	private readonly ICurrentSessionService _currentSessionService;
	private readonly IAuthorizationPolicyService _authorizationPolicyService;
	private readonly IOperationLogRepository _operationLogRepository;
	private readonly IUtcClock _clock;
	private readonly ILogger<SubmitRefundUseCase> _logger;

	public SubmitRefundUseCase(
		ICartSessionRepository cartSessionRepository,
		ICheckoutPaymentAttemptRepository checkoutPaymentAttemptRepository,
		IRefundRepository refundRepository,
		ICurrentSessionService currentSessionService,
		IAuthorizationPolicyService authorizationPolicyService,
		IOperationLogRepository operationLogRepository,
		IUtcClock clock,
		ILogger<SubmitRefundUseCase> logger)
	{
		_cartSessionRepository = cartSessionRepository;
		_checkoutPaymentAttemptRepository = checkoutPaymentAttemptRepository;
		_refundRepository = refundRepository;
		_currentSessionService = currentSessionService;
		_authorizationPolicyService = authorizationPolicyService;
		_operationLogRepository = operationLogRepository;
		_clock = clock;
		_logger = logger;
	}

	public async Task<AppResult<SubmitRefundResultDto>> ExecuteAsync(SubmitRefundCommand command, CancellationToken ct = default)
	{
		if (command.AmountCents <= 0)
		{
			return AppResult<SubmitRefundResultDto>.Failure(
				RefundWorkflowConstants.ErrorAmountInvalid,
				RefundWorkflowConstants.SafeAmountInvalidMessage);
		}

		var session = _currentSessionService.GetCurrent();
		if (session is null)
		{
			return AppResult<SubmitRefundResultDto>.Failure(
				RefundWorkflowConstants.ErrorAuthForbidden,
				RefundWorkflowConstants.SafeAuthForbiddenMessage);
		}

		var canInitiate = _authorizationPolicyService.HasPermission(session.Role, RolePermissions.CheckoutRefundInitiate);
		if (!canInitiate)
		{
			await AppendDeniedAsync(
				session,
				command.CartSessionId,
				command,
				RefundWorkflowConstants.ErrorAuthForbidden,
				ct);

			return AppResult<SubmitRefundResultDto>.Failure(
				RefundWorkflowConstants.ErrorAuthForbidden,
				RefundWorkflowConstants.SafeAuthForbiddenMessage);
		}

		if (command.RequestedPath == RefundPath.ApprovalRequired && string.IsNullOrWhiteSpace(command.Reason))
		{
			return AppResult<SubmitRefundResultDto>.Failure(
				RefundWorkflowConstants.ErrorReasonRequired,
				RefundWorkflowConstants.SafeReasonRequiredMessage);
		}

		try
		{
			var existing = await _refundRepository.GetByOperationIdAsync(command.Context.OperationId, ct);
			if (existing is not null)
			{
				return AppResult<SubmitRefundResultDto>.Success(
					Map(existing, RefundWorkflowConstants.RefundAlreadyProcessedMessage),
					RefundWorkflowConstants.RefundAlreadyProcessedMessage);
			}

			var cart = await _cartSessionRepository.GetByIdAsync(command.CartSessionId, ct);
			if (cart is null)
			{
				return AppResult<SubmitRefundResultDto>.Failure(
					RefundWorkflowConstants.ErrorTargetNotFound,
					RefundWorkflowConstants.SafeTargetNotFoundMessage);
			}

			var approvedAttempts = (await _checkoutPaymentAttemptRepository.ListByCartSessionAsync(command.CartSessionId, ct))
				.Where(x => x.AuthorizationStatus == CheckoutPaymentAuthorizationStatus.Approved)
				.ToList();

			if (approvedAttempts.Count == 0)
			{
				return AppResult<SubmitRefundResultDto>.Failure(
					RefundWorkflowConstants.ErrorNotEligible,
					RefundWorkflowConstants.SafeNotEligibleMessage);
			}

			var approvedTotal = approvedAttempts.Sum(x => x.AmountCents);
			var refundedTotal = await _refundRepository.SumCompletedAmountByCartSessionAsync(command.CartSessionId, ct);
			var refundableBalance = approvedTotal - refundedTotal;

			if (refundableBalance <= 0)
			{
				return AppResult<SubmitRefundResultDto>.Failure(
					RefundWorkflowConstants.ErrorAlreadyCompleted,
					RefundWorkflowConstants.SafeAlreadyCompletedMessage);
			}

			if (command.AmountCents > refundableBalance)
			{
				await AppendDeniedAsync(
					session,
					command.CartSessionId,
					command,
					RefundWorkflowConstants.ErrorAmountInvalid,
					ct);

				return AppResult<SubmitRefundResultDto>.Failure(
					RefundWorkflowConstants.ErrorAmountInvalid,
					RefundWorkflowConstants.SafeAmountInvalidMessage);
			}

			var canApprove = _authorizationPolicyService.HasPermission(session.Role, RolePermissions.CheckoutRefundApprove);
			if (command.RequestedPath == RefundPath.Direct && !canApprove)
			{
				await AppendDeniedAsync(
					session,
					command.CartSessionId,
					command,
					RefundWorkflowConstants.ErrorPathForbidden,
					ct);

				return AppResult<SubmitRefundResultDto>.Failure(
					RefundWorkflowConstants.ErrorPathForbidden,
					RefundWorkflowConstants.SafePathForbiddenMessage);
			}

			var reason = string.IsNullOrWhiteSpace(command.Reason)
				? RefundWorkflowConstants.DefaultReasonPlaceholder
				: command.Reason.Trim();
			var currencyCode = approvedAttempts.First().CurrencyCode;

			if (command.RequestedPath == RefundPath.ApprovalRequired && !canApprove)
			{
				var pending = RefundRecord.Create(
					Guid.NewGuid(),
					command.CartSessionId,
					command.Context.OperationId,
					RefundStatus.PendingApproval,
					RefundPath.ApprovalRequired,
					command.AmountCents,
					currencyCode,
					reason,
					session.StaffId,
					session.Role.ToString(),
					_clock.UtcNow);

				await _refundRepository.AddAsyncWithBalanceCheckAsync(pending, approvedTotal, ct);
				await AppendInitiatedAsync(session, pending, command.Context, ct);
				await _operationLogRepository.AppendAsync(
					SecurityAuditEventTypes.RefundApprovalRequested,
					pending.CartSessionId.ToString(),
					new RefundAuditPayload(
						session.StaffId,
						session.Role.ToString(),
						pending.CartSessionId.ToString(),
						reason,
						command.Context.OperationId,
						command.Context.OccurredUtc,
						pending.AmountCents,
						pending.CurrencyCode,
						RefundWorkflowConstants.RefundApprovalRequestedMessage),
					command.Context,
					cancellationToken: ct);

				return AppResult<SubmitRefundResultDto>.Success(
					Map(pending, RefundWorkflowConstants.RefundApprovalRequestedMessage),
					RefundWorkflowConstants.RefundApprovalRequestedMessage);
			}

			var completedAt = _clock.UtcNow;
			var completed = RefundRecord.Create(
				Guid.NewGuid(),
				command.CartSessionId,
				command.Context.OperationId,
				RefundStatus.Completed,
				command.RequestedPath,
				command.AmountCents,
				currencyCode,
				reason,
				session.StaffId,
				session.Role.ToString(),
				completedAt,
				completedAt);

			await _refundRepository.AddAsyncWithBalanceCheckAsync(completed, approvedTotal, ct);
			await AppendInitiatedAsync(session, completed, command.Context, ct);
			await _operationLogRepository.AppendAsync(
				SecurityAuditEventTypes.RefundCompleted,
				completed.CartSessionId.ToString(),
				new RefundAuditPayload(
					session.StaffId,
					session.Role.ToString(),
					completed.CartSessionId.ToString(),
					reason,
					command.Context.OperationId,
					command.Context.OccurredUtc,
					completed.AmountCents,
					completed.CurrencyCode,
					RefundWorkflowConstants.RefundCompletedMessage),
				command.Context,
				cancellationToken: ct);

			return AppResult<SubmitRefundResultDto>.Success(
				Map(completed, RefundWorkflowConstants.RefundCompletedMessage),
				RefundWorkflowConstants.RefundCompletedMessage);
		}
		catch (InvalidOperationException ex)
		{
			await AppendDeniedAsync(
				session,
				command.CartSessionId,
				command,
				RefundWorkflowConstants.ErrorAmountInvalid,
				ct);

			_logger.LogWarning(ex, "Refund balance check rejected for cart {CartSessionId} op {OperationId}", command.CartSessionId, command.Context.OperationId);
			return AppResult<SubmitRefundResultDto>.Failure(
				RefundWorkflowConstants.ErrorAmountInvalid,
				RefundWorkflowConstants.SafeAmountInvalidMessage);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Refund processing failed for cart {CartSessionId} op {OperationId}", command.CartSessionId, command.Context.OperationId);
			return AppResult<SubmitRefundResultDto>.Failure(
				RefundWorkflowConstants.ErrorCommitFailed,
				RefundWorkflowConstants.SafeCommitFailedMessage);
		}
	}

	private async Task AppendInitiatedAsync(
		CurrentSession session,
		RefundRecord record,
		OperationContext operationContext,
		CancellationToken ct)
	{
		await _operationLogRepository.AppendAsync(
			SecurityAuditEventTypes.RefundInitiated,
			record.CartSessionId.ToString(),
			new RefundAuditPayload(
				session.StaffId,
				session.Role.ToString(),
				record.CartSessionId.ToString(),
				record.Reason,
				operationContext.OperationId,
				operationContext.OccurredUtc,
				record.AmountCents,
				record.CurrencyCode,
				RefundWorkflowConstants.RefundInitiationRecordedMessage),
			operationContext,
			cancellationToken: ct);
	}

	private async Task AppendDeniedAsync(
		CurrentSession session,
		Guid cartSessionId,
		SubmitRefundCommand command,
		string denialReasonCode,
		CancellationToken ct)
	{
		await _operationLogRepository.AppendAsync(
			SecurityAuditEventTypes.RefundDenied,
			cartSessionId.ToString(),
			new RefundDeniedAuditPayload(
				session.StaffId,
				session.Role.ToString(),
				cartSessionId.ToString(),
				string.IsNullOrWhiteSpace(command.Reason) ? RefundWorkflowConstants.DefaultReasonPlaceholder : command.Reason.Trim(),
				command.Context.OperationId,
				command.Context.OccurredUtc,
				command.AmountCents,
				denialReasonCode),
			command.Context,
			cancellationToken: ct);
	}

	private static SubmitRefundResultDto Map(RefundRecord record, string message) =>
		new(
			record.OperationId,
			record.CartSessionId,
			record.Status,
			record.Path,
			record.ActorStaffId,
			record.ActorRole,
			record.AmountCents,
			record.CurrencyCode,
			message);
}

internal sealed record RefundAuditPayload(
	Guid ActorStaffId,
	string ActorRole,
	string TargetReference,
	string Reason,
	Guid OperationId,
	DateTime OccurredUtc,
	long RefundAmountCents,
	string CurrencyCode,
	string Notes);

internal sealed record RefundDeniedAuditPayload(
	Guid ActorStaffId,
	string ActorRole,
	string TargetReference,
	string Reason,
	Guid OperationId,
	DateTime OccurredUtc,
	long RefundAmountCents,
	string DenialReasonCode);