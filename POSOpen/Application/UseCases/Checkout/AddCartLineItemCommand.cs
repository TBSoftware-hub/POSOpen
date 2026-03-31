using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Checkout;

public sealed record AddCartLineItemCommand(
    Guid CartSessionId,
    string Description,
    FulfillmentContext FulfillmentContext,
    Guid? ReferenceId,
    int Quantity,
    long UnitAmountCents,
    string CurrencyCode = "USD");
