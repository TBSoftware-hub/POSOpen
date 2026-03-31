namespace POSOpen.Application.UseCases.Checkout;

public static class CartCheckoutConstants
{
    public const string ErrorCartNotFound = "CART_NOT_FOUND";
    public const string ErrorCartNotOpen = "CART_NOT_OPEN";
    public const string ErrorInvalidQuantity = "INVALID_QUANTITY";
    public const string ErrorLineItemNotFound = "LINE_ITEM_NOT_FOUND";
    public const string ErrorStaffNotAuthenticated = "STAFF_NOT_AUTHENTICATED";

    public const string SafeCartNotFoundMessage = "Cart session could not be found.";
    public const string SafeCartNotOpenMessage = "This cart is no longer open and cannot be modified.";
    public const string SafeInvalidQuantityMessage = "Quantity must be at least 1.";
    public const string SafeLineItemNotFoundMessage = "The specified item was not found in the cart.";
    public const string SafeStaffNotAuthenticatedMessage = "You must be signed in to manage a cart.";
}
