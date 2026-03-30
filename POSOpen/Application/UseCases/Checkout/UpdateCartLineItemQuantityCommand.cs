namespace POSOpen.Application.UseCases.Checkout;

public sealed record UpdateCartLineItemQuantityCommand(Guid CartSessionId, Guid LineItemId, int NewQuantity);
