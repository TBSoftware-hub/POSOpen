using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Checkout;

public sealed class RemoveCartLineItemUseCase
{
    private readonly ICartSessionRepository _cartSessionRepository;
    private readonly IUtcClock _clock;

    public RemoveCartLineItemUseCase(ICartSessionRepository cartSessionRepository, IUtcClock clock)
    {
        _cartSessionRepository = cartSessionRepository;
        _clock = clock;
    }

    public async Task<AppResult<CartSessionDto>> ExecuteAsync(
        RemoveCartLineItemCommand command,
        CancellationToken ct = default)
    {
        var cart = await _cartSessionRepository.GetByIdAsync(command.CartSessionId, ct);
        if (cart is null)
        {
            return AppResult<CartSessionDto>.Failure(
                CartCheckoutConstants.ErrorCartNotFound,
                CartCheckoutConstants.SafeCartNotFoundMessage);
        }

        if (cart.Status != CartStatus.Open)
        {
            return AppResult<CartSessionDto>.Failure(
                CartCheckoutConstants.ErrorCartNotOpen,
                CartCheckoutConstants.SafeCartNotOpenMessage);
        }

        var item = cart.LineItems.FirstOrDefault(i => i.Id == command.LineItemId);
        if (item is null)
        {
            return AppResult<CartSessionDto>.Failure(
                CartCheckoutConstants.ErrorLineItemNotFound,
                CartCheckoutConstants.SafeLineItemNotFoundMessage);
        }

        var updatedCart = await _cartSessionRepository.RemoveLineItemAsync(cart.Id, command.LineItemId, _clock.UtcNow, ct);
        if (updatedCart is null)
        {
            return AppResult<CartSessionDto>.Failure(
                CartCheckoutConstants.ErrorCartNotFound,
                CartCheckoutConstants.SafeCartNotFoundMessage);
        }

        return AppResult<CartSessionDto>.Success(
                GetOrCreateCartSessionUseCase.MapToDto(updatedCart),
            "Item removed from cart.");
    }
}
