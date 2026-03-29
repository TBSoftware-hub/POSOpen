using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Results;
using POSOpen.Application.Security;

namespace POSOpen.Application.UseCases.Security;

public sealed class SubmitOverrideUseCase
{
	private const string OverrideActionCommittedEvent = "OverrideActionCommitted";

	private readonly ICurrentSessionService _currentSessionService;
	private readonly IAuthorizationPolicyService _authorizationPolicyService;
	private readonly IOperationLogRepository _operationLogRepository;
	private readonly ILogger<SubmitOverrideUseCase> _logger;

	public SubmitOverrideUseCase(
		ICurrentSessionService currentSessionService,
		IAuthorizationPolicyService authorizationPolicyService,
		IOperationLogRepository operationLogRepository,
		ILogger<SubmitOverrideUseCase> logger)
	{
		_currentSessionService = currentSessionService;
		_authorizationPolicyService = authorizationPolicyService;
		_operationLogRepository = operationLogRepository;
		_logger = logger;
	}

	public async Task<AppResult<SubmitOverrideResultDto>> ExecuteAsync(
		SubmitOverrideCommand command,
		CancellationToken ct = default)
	{
		// Validate context exists
		if (string.IsNullOrWhiteSpace(command.ActionKey) || string.IsNullOrWhiteSpace(command.TargetReference))
		{
			return AppResult<SubmitOverrideResultDto>.Failure(
				SubmitOverrideConstants.ErrorContextInvalid,
				SubmitOverrideConstants.SafeContextInvalidMessage);
		}

		// Validate reason
		if (string.IsNullOrWhiteSpace(command.Reason))
		{
			return AppResult<SubmitOverrideResultDto>.Failure(
				SubmitOverrideConstants.ErrorReasonRequired,
				SubmitOverrideConstants.SafeReasonRequiredMessage);
		}

		// Check current session (trust only authenticated session, never UI claims)
		var session = _currentSessionService.GetCurrent();
		if (session is null)
		{
			return AppResult<SubmitOverrideResultDto>.Failure(
				SubmitOverrideConstants.ErrorAuthForbidden,
				SubmitOverrideConstants.SafeAuthForbiddenMessage);
		}

		// Check authorization using centralized policy service
		if (!_authorizationPolicyService.HasPermission(session.Role, RolePermissions.SecurityOverrideExecute))
		{
			_logger.LogWarning(
				"Override attempt by unauthorized role: StaffId={StaffId}, Role={Role}, ActionKey={ActionKey}",
				session.StaffId,
				session.Role,
				command.ActionKey);

			return AppResult<SubmitOverrideResultDto>.Failure(
				SubmitOverrideConstants.ErrorAuthForbidden,
				SubmitOverrideConstants.SafeAuthForbiddenMessage);
		}

		// Trim reason for immutable logging
		var trimmedReason = command.Reason.Trim();

		try
		{
			// Append immutable override event
			await _operationLogRepository.AppendAsync(
				OverrideActionCommittedEvent,
				session.StaffId.ToString(),
				new OverrideActionCommittedPayload(
					StaffId: session.StaffId,
					StaffRole: session.Role.ToString(),
					ActionKey: command.ActionKey,
					TargetReference: command.TargetReference,
					Reason: trimmedReason,
					OperationId: command.Context.OperationId,
					OccurredUtc: command.Context.OccurredUtc),
				command.Context,
				cancellationToken: ct);

			_logger.LogInformation(
				"Override action committed: StaffId={StaffId}, Role={Role}, ActionKey={ActionKey}, OperationId={OperationId}",
				session.StaffId,
				session.Role,
				command.ActionKey,
				command.Context.OperationId);

			return AppResult<SubmitOverrideResultDto>.Success(
				new SubmitOverrideResultDto(
					command.Context.OperationId,
					command.ActionKey,
					command.TargetReference,
					session.StaffId,
					session.Role),
				SubmitOverrideConstants.OverrideSucceededMessage);
		}
		catch (Exception ex)
		{
			_logger.LogError(
				ex,
				"Failed to commit override action: StaffId={StaffId}, ActionKey={ActionKey}, OperationId={OperationId}",
				session.StaffId,
				command.ActionKey,
				command.Context.OperationId);

			return AppResult<SubmitOverrideResultDto>.Failure(
				SubmitOverrideConstants.ErrorCommitFailed,
				SubmitOverrideConstants.SafeCommitFailedMessage,
				ex.Message);
		}
	}
}

internal sealed record OverrideActionCommittedPayload(
	Guid StaffId,
	string StaffRole,
	string ActionKey,
	string TargetReference,
	string Reason,
	Guid OperationId,
	DateTime OccurredUtc);
