using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Checkout;

public sealed record CartLineItemDto(
    Guid Id,
    Guid CartSessionId,
    string Description,
    FulfillmentContext FulfillmentContext,
    Guid? ReferenceId,
    int Quantity,
    long UnitAmountCents,
    long LineTotalCents,
    string CurrencyCode,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CartSessionDto(
    Guid Id,
    Guid? FamilyId,
    Guid StaffId,
    CartStatus Status,
    long TotalAmountCents,
    IReadOnlyList<CartLineItemDto> LineItems,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
