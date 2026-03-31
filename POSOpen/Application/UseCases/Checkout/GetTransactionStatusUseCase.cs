using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Results;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Checkout;

public sealed class GetTransactionStatusUseCase
{
	private readonly ICartSessionRepository _cartSessionRepository;
	private readonly ICheckoutPaymentAttemptRepository _checkoutPaymentAttemptRepository;

	public GetTransactionStatusUseCase(
		ICartSessionRepository cartSessionRepository,
		ICheckoutPaymentAttemptRepository checkoutPaymentAttemptRepository)
	{
		_cartSessionRepository = cartSessionRepository;
		_checkoutPaymentAttemptRepository = checkoutPaymentAttemptRepository;
	}

	public async Task<AppResult<TransactionStatusDto>> ExecuteAsync(Guid cartSessionId, CancellationToken ct = default)
	{
		var cart = await _cartSessionRepository.GetByIdAsync(cartSessionId, ct);
		if (cart is null)
		{
			return AppResult<TransactionStatusDto>.Failure(
				CartCheckoutConstants.ErrorCartNotFound,
				CartCheckoutConstants.SafeCartNotFoundMessage);
		}

		var attempts = await _checkoutPaymentAttemptRepository.ListByCartSessionAsync(cartSessionId, ct);

		var approvedAttempt = attempts.FirstOrDefault(
			a => a.AuthorizationStatus == CheckoutPaymentAuthorizationStatus.Approved);

		var (status, statusMessage, nextStepsMessage) = DetermineStatus(cart.Status, approvedAttempt is not null);

		var result = new TransactionStatusDto(
			CartSessionId: cartSessionId,
			TransactionStatus: status,
			LastOperationId: null,
			StatusMessage: statusMessage,
			NextStepsMessage: nextStepsMessage);

		return AppResult<TransactionStatusDto>.Success(result, statusMessage);
	}

	private static (TransactionStatus status, string statusMessage, string nextStepsMessage) DetermineStatus(
		CartStatus cartStatus,
		bool hasApprovedPayment)
	{
		if (hasApprovedPayment)
		{
			return (
				TransactionStatus.CompletedOnline,
				"Payment approved. Transaction complete.",
				"Hand receipt to customer and complete the session.");
		}

		if (cartStatus == CartStatus.Open)
		{
			return (
				TransactionStatus.DeferredPayment,
				"No payment recorded. Cart is still open.",
				"Collect payment before closing the session, or escalate for override.");
		}

		return (
			TransactionStatus.Error,
			"Cart is closed but no approved payment was found.",
			"Review the transaction log and escalate if needed.");
	}
}
