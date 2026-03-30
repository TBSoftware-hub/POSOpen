namespace POSOpen.Application.UseCases.Checkout;

public sealed record RemoveCartLineItemCommand(Guid CartSessionId, Guid LineItemId);
