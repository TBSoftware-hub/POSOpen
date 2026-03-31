using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Checkout;

public sealed class UpdateCartLineItemQuantityUseCase
{
    private readonly ICartSessionRepository _cartSessionRepository;
    private readonly IUtcClock _clock;

    public UpdateCartLineItemQuantityUseCase(ICartSessionRepository cartSessionRepository, IUtcClock clock)
    {
        _cartSessionRepository = cartSessionRepository;
        _clock = clock;
    }

    public async Task<AppResult<CartSessionDto>> ExecuteAsync(
        UpdateCartLineItemQuantityCommand command,
        CancellationToken ct = default)
    {
        if (command.NewQuantity < 1)
        {
            return AppResult<CartSessionDto>.Failure(
                CartCheckoutConstants.ErrorInvalidQuantity,
                CartCheckoutConstants.SafeInvalidQuantityMessage);
        }

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

        var updatedCart = await _cartSessionRepository.UpdateLineItemQuantityAsync(cart.Id, command.LineItemId, command.NewQuantity, _clock.UtcNow, ct);
        if (updatedCart is null)
        {
            return AppResult<CartSessionDto>.Failure(
                CartCheckoutConstants.ErrorCartNotFound,
                CartCheckoutConstants.SafeCartNotFoundMessage);
        }

        return AppResult<CartSessionDto>.Success(
                GetOrCreateCartSessionUseCase.MapToDto(updatedCart),
            "Item quantity updated.");
    }
}
