using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Checkout;

public sealed class AddCartLineItemUseCase
{
    private readonly ICartSessionRepository _cartSessionRepository;
    private readonly IUtcClock _clock;

    public AddCartLineItemUseCase(ICartSessionRepository cartSessionRepository, IUtcClock clock)
    {
        _cartSessionRepository = cartSessionRepository;
        _clock = clock;
    }

    public async Task<AppResult<CartSessionDto>> ExecuteAsync(
        AddCartLineItemCommand command,
        CancellationToken ct = default)
    {
        if (command.Quantity <= 0)
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

        var lineItem = CartLineItem.Create(
            Guid.NewGuid(),
            cart.Id,
            command.Description,
            command.FulfillmentContext,
            command.ReferenceId,
            command.Quantity,
            command.UnitAmountCents,
            command.CurrencyCode,
            _clock.UtcNow);

        var updatedCart = await _cartSessionRepository.AddLineItemAsync(cart.Id, lineItem, _clock.UtcNow, ct);
        if (updatedCart is null)
        {
            return AppResult<CartSessionDto>.Failure(
                CartCheckoutConstants.ErrorCartNotFound,
                CartCheckoutConstants.SafeCartNotFoundMessage);
        }

        return AppResult<CartSessionDto>.Success(
                GetOrCreateCartSessionUseCase.MapToDto(updatedCart),
            "Item added to cart.");
    }
}
