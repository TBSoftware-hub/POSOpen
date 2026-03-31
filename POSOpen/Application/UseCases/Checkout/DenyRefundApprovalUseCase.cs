using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Application.Security;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Checkout;

public sealed class DenyRefundApprovalUseCase
{
	private readonly IRefundRepository _refundRepository;
	private readonly ICurrentSessionService _currentSessionService;
	private readonly IAuthorizationPolicyService _authorizationPolicyService;
	private readonly IOperationLogRepository _operationLogRepository;
	private readonly IUtcClock _clock;
	private readonly ILogger<DenyRefundApprovalUseCase> _logger;

	public DenyRefundApprovalUseCase(
		IRefundRepository refundRepository,
		ICurrentSessionService currentSessionService,
		IAuthorizationPolicyService authorizationPolicyService,
		IOperationLogRepository operationLogRepository,
		IUtcClock clock,
		ILogger<DenyRefundApprovalUseCase> logger)
	{
		_refundRepository = refundRepository;
		_currentSessionService = currentSessionService;
		_authorizationPolicyService = authorizationPolicyService;
		_operationLogRepository = operationLogRepository;
		_clock = clock;
		_logger = logger;
	}

	public async Task<AppResult<SubmitRefundResultDto>> ExecuteAsync(
		DenyRefundApprovalCommand command,
		CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(command.Reason))
		{
			return AppResult<SubmitRefundResultDto>.Failure(
				RefundWorkflowConstants.ErrorReasonRequired,
				RefundWorkflowConstants.SafeReasonRequiredMessage);
		}

		var session = _currentSessionService.GetCurrent();
		if (session is null)
		{
			return AppResult<SubmitRefundResultDto>.Failure(
				RefundWorkflowConstants.ErrorAuthForbidden,
				RefundWorkflowConstants.SafeAuthForbiddenMessage);
		}

		if (!_authorizationPolicyService.HasPermission(session.Role, RolePermissions.CheckoutRefundApprove))
		{
			return AppResult<SubmitRefundResultDto>.Failure(
				RefundWorkflowConstants.ErrorAuthForbidden,
				RefundWorkflowConstants.SafeAuthForbiddenMessage);
		}

		var refund = await _refundRepository.GetByIdAsync(command.RefundId, ct);
		if (refund is null)
		{
			return AppResult<SubmitRefundResultDto>.Failure(
				RefundWorkflowConstants.ErrorTargetNotFound,
				RefundWorkflowConstants.SafeTargetNotFoundMessage);
		}

		if (refund.Status != RefundStatus.PendingApproval)
		{
			return AppResult<SubmitRefundResultDto>.Failure(
				RefundWorkflowConstants.ErrorApprovalStateInvalid,
				RefundWorkflowConstants.SafeApprovalStateInvalidMessage);
		}

		var deniedAt = _clock.UtcNow;
		var reason = command.Reason.Trim();
		var denied = RefundRecord.Create(
			refund.Id,
			refund.CartSessionId,
			refund.OperationId,
			RefundStatus.ApprovalDenied,
			refund.Path,
			refund.AmountCents,
			refund.CurrencyCode,
			reason,
			session.StaffId,
			session.Role.ToString(),
			refund.CreatedAtUtc,
			deniedAt);

		try
		{
			await _refundRepository.UpdateAsync(denied, ct);
			await _operationLogRepository.AppendAsync(
				SecurityAuditEventTypes.RefundApprovalDenied,
				denied.CartSessionId.ToString(),
				new RefundApprovalDeniedAuditPayload(
					session.StaffId,
					session.Role.ToString(),
					denied.CartSessionId.ToString(),
					reason,
					command.Context.OperationId,
					command.Context.OccurredUtc,
					denied.AmountCents,
					denied.CurrencyCode,
					RefundWorkflowConstants.RefundApprovalDeniedMessage),
				command.Context,
				cancellationToken: ct);

			return AppResult<SubmitRefundResultDto>.Success(
				new SubmitRefundResultDto(
					command.Context.OperationId,
					denied.CartSessionId,
					denied.Status,
					denied.Path,
					denied.ActorStaffId,
					denied.ActorRole,
					denied.AmountCents,
					denied.CurrencyCode,
					RefundWorkflowConstants.RefundApprovalDeniedMessage),
				RefundWorkflowConstants.RefundApprovalDeniedMessage);
		}
		catch (Exception ex)
		{
			_logger.LogError(
				ex,
				"Failed to deny refund approval for RefundId={RefundId}, OperationId={OperationId}",
				command.RefundId,
				command.Context.OperationId);

			return AppResult<SubmitRefundResultDto>.Failure(
				RefundWorkflowConstants.ErrorCommitFailed,
				RefundWorkflowConstants.SafeCommitFailedMessage);
		}
	}
}

internal sealed record RefundApprovalDeniedAuditPayload(
	Guid ActorStaffId,
	string ActorRole,
	string TargetReference,
	string Reason,
	Guid OperationId,
	DateTime OccurredUtc,
	long RefundAmountCents,
	string CurrencyCode,
	string Notes);
