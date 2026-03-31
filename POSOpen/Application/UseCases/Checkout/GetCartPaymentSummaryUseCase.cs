using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Results;

namespace POSOpen.Application.UseCases.Checkout;

public sealed class GetCartPaymentSummaryUseCase
{
	private readonly ICartSessionRepository _cartSessionRepository;

	public GetCartPaymentSummaryUseCase(ICartSessionRepository cartSessionRepository)
	{
		_cartSessionRepository = cartSessionRepository;
	}

	public async Task<AppResult<CartPaymentSummaryDto>> ExecuteAsync(Guid cartSessionId, CancellationToken ct = default)
	{
		var cart = await _cartSessionRepository.GetByIdAsync(cartSessionId, ct);
		if (cart is null)
		{
			return AppResult<CartPaymentSummaryDto>.Failure(
				CartCheckoutConstants.ErrorCartNotFound,
				CartCheckoutConstants.SafeCartNotFoundMessage);
		}

		if (cart.LineItems.Count == 0)
		{
			return AppResult<CartPaymentSummaryDto>.Failure(
				CartCheckoutConstants.ErrorCartEmpty,
				CartCheckoutConstants.SafeCartEmptyMessage);
		}

		var currencyCode = cart.LineItems.FirstOrDefault()?.CurrencyCode ?? "USD";
		return AppResult<CartPaymentSummaryDto>.Success(
			new CartPaymentSummaryDto(cart.Id, cart.TotalAmountCents, currencyCode),
			"Payment summary loaded.");
	}
}