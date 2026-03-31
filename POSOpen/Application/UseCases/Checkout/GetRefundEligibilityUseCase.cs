using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Results;
using POSOpen.Application.Security;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Checkout;

public sealed class GetRefundEligibilityUseCase
{
	private readonly ICartSessionRepository _cartSessionRepository;
	private readonly ICheckoutPaymentAttemptRepository _checkoutPaymentAttemptRepository;
	private readonly IRefundRepository _refundRepository;
	private readonly ICurrentSessionService _currentSessionService;
	private readonly IAuthorizationPolicyService _authorizationPolicyService;

	public GetRefundEligibilityUseCase(
		ICartSessionRepository cartSessionRepository,
		ICheckoutPaymentAttemptRepository checkoutPaymentAttemptRepository,
		IRefundRepository refundRepository,
		ICurrentSessionService currentSessionService,
		IAuthorizationPolicyService authorizationPolicyService)
	{
		_cartSessionRepository = cartSessionRepository;
		_checkoutPaymentAttemptRepository = checkoutPaymentAttemptRepository;
		_refundRepository = refundRepository;
		_currentSessionService = currentSessionService;
		_authorizationPolicyService = authorizationPolicyService;
	}

	public async Task<AppResult<RefundEligibilityDto>> ExecuteAsync(Guid cartSessionId, CancellationToken ct = default)
	{
		var session = _currentSessionService.GetCurrent();
		if (session is null)
		{
			return AppResult<RefundEligibilityDto>.Failure(
				RefundWorkflowConstants.ErrorAuthForbidden,
				RefundWorkflowConstants.SafeAuthForbiddenMessage);
		}

		if (!_authorizationPolicyService.HasPermission(session.Role, RolePermissions.CheckoutRefundInitiate))
		{
			return AppResult<RefundEligibilityDto>.Failure(
				RefundWorkflowConstants.ErrorAuthForbidden,
				RefundWorkflowConstants.SafeAuthForbiddenMessage);
		}

		var cart = await _cartSessionRepository.GetByIdAsync(cartSessionId, ct);
		if (cart is null)
		{
			return AppResult<RefundEligibilityDto>.Success(
				BuildBlocked(
					cartSessionId,
					RefundWorkflowConstants.ErrorTargetNotFound,
					RefundWorkflowConstants.SafeTargetNotFoundMessage),
				RefundWorkflowConstants.SafeTargetNotFoundMessage);
		}

		var approvedAttempts = (await _checkoutPaymentAttemptRepository.ListByCartSessionAsync(cartSessionId, ct))
			.Where(x => x.AuthorizationStatus == CheckoutPaymentAuthorizationStatus.Approved)
			.ToList();

		if (approvedAttempts.Count == 0)
		{
			return AppResult<RefundEligibilityDto>.Success(
				BuildBlocked(
					cartSessionId,
					RefundWorkflowConstants.ErrorNotEligible,
					RefundWorkflowConstants.SafeNotEligibleMessage),
				RefundWorkflowConstants.SafeNotEligibleMessage);
		}

		var approvedTotal = approvedAttempts.Sum(x => x.AmountCents);
		
		// Ensure all approved amounts are valid (non-zero total)
		if (approvedTotal <= 0)
		{
			return AppResult<RefundEligibilityDto>.Success(
				BuildBlocked(
					cartSessionId,
					RefundWorkflowConstants.ErrorNotEligible,
					RefundWorkflowConstants.SafeNotEligibleMessage),
				RefundWorkflowConstants.SafeNotEligibleMessage);
		}
		
		var refundedTotal = await _refundRepository.SumCompletedAmountByCartSessionAsync(cartSessionId, ct);
		var refundableBalance = approvedTotal - refundedTotal;
		if (refundableBalance <= 0)
		{
			return AppResult<RefundEligibilityDto>.Success(
				BuildBlocked(
					cartSessionId,
					RefundWorkflowConstants.ErrorAlreadyCompleted,
					RefundWorkflowConstants.SafeAlreadyCompletedMessage),
				RefundWorkflowConstants.SafeAlreadyCompletedMessage);
		}

		var canApprove = _authorizationPolicyService.HasPermission(session.Role, RolePermissions.CheckoutRefundApprove);
		var allowedPaths = canApprove
			? (IReadOnlyList<RefundPath>)[RefundPath.Direct, RefundPath.ApprovalRequired]
			: [RefundPath.ApprovalRequired];

		var currencyCode = approvedAttempts.First().CurrencyCode;
		var dto = new RefundEligibilityDto(
			cartSessionId,
			true,
			refundableBalance,
			currencyCode,
			allowedPaths,
			null,
			RefundWorkflowConstants.EligibleRefundAvailableMessage);

		return AppResult<RefundEligibilityDto>.Success(dto, dto.UserMessage);
	}

	private static RefundEligibilityDto BuildBlocked(
		Guid cartSessionId,
		string reasonCode,
		string message) =>
		new(
			cartSessionId,
			false,
			0,
			"USD",
			[],
			reasonCode,
			message);
}