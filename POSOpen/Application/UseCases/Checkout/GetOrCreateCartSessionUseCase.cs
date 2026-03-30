using Microsoft.EntityFrameworkCore;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Checkout;

public sealed class GetOrCreateCartSessionUseCase
{
    private readonly ICartSessionRepository _cartSessionRepository;
    private readonly IAppStateService _appStateService;
    private readonly IUtcClock _clock;

    public GetOrCreateCartSessionUseCase(
        ICartSessionRepository cartSessionRepository,
        IAppStateService appStateService,
        IUtcClock clock)
    {
        _cartSessionRepository = cartSessionRepository;
        _appStateService = appStateService;
        _clock = clock;
    }

    public async Task<AppResult<CartSessionDto>> ExecuteAsync(CancellationToken ct = default)
    {
        if (_appStateService.CurrentStaffId is not { } staffId)
        {
            return AppResult<CartSessionDto>.Failure(
                CartCheckoutConstants.ErrorStaffNotAuthenticated,
                CartCheckoutConstants.SafeStaffNotAuthenticatedMessage);
        }

        var existingCart = await _cartSessionRepository.GetOpenCartForStaffAsync(staffId, ct);
        if (existingCart is not null)
        {
            return AppResult<CartSessionDto>.Success(MapToDto(existingCart), "Cart loaded.");
        }

        var newCart = CartSession.Create(Guid.NewGuid(), null, staffId, _clock.UtcNow);
        try
        {
            await _cartSessionRepository.CreateAsync(newCart, ct);
        }
        catch (DbUpdateException)
        {
            // Another request may have created the open cart concurrently.
            var concurrentCart = await _cartSessionRepository.GetOpenCartForStaffAsync(staffId, ct);
            if (concurrentCart is not null)
            {
                return AppResult<CartSessionDto>.Success(MapToDto(concurrentCart), "Cart loaded.");
            }

            return AppResult<CartSessionDto>.Failure(
                CartCheckoutConstants.ErrorCartNotFound,
                CartCheckoutConstants.SafeCartNotFoundMessage);
        }

        return AppResult<CartSessionDto>.Success(MapToDto(newCart), "New cart created.");
    }

    internal static CartSessionDto MapToDto(CartSession cart) =>
        new(
            cart.Id,
            cart.FamilyId,
            cart.StaffId,
            cart.Status,
            cart.TotalAmountCents,
            cart.LineItems
                .OrderBy(i => i.CreatedAtUtc)
                .Select(i => new CartLineItemDto(
                    i.Id,
                    i.CartSessionId,
                    i.Description,
                    i.FulfillmentContext,
                    i.ReferenceId,
                    i.Quantity,
                    i.UnitAmountCents,
                    i.LineTotalCents,
                    i.CurrencyCode,
                    i.CreatedAtUtc,
                    i.UpdatedAtUtc))
                .ToList(),
            cart.CreatedAtUtc,
            cart.UpdatedAtUtc);
}
