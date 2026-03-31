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

    public const string ErrorCartEmpty = "CART_EMPTY";
    public const string SafeCartEmptyMessage =
        "The cart is empty. Add at least one item before proceeding to payment.";

    public const string ErrorCateringWithoutDeposit = "CATERING_WITHOUT_PARTY_DEPOSIT";
    public const string SafeCateringWithoutDepositMessage =
        "Catering add-ons require a party deposit in the cart.";

    public const string ErrorMultipleDeposits = "MULTIPLE_PARTY_DEPOSITS";
    public const string ErrorScannerUnavailable = "SCANNER_UNAVAILABLE";
    public const string ErrorScannerUnresolved = "SCANNER_UNRESOLVED";
    public const string ErrorCardReaderUnavailable = "CARD_READER_UNAVAILABLE";

    public const string SafeMultipleDepositsMessage =
        "Only one party deposit is allowed per cart.";
    public const string SafeScannerUnavailableMessage =
        "Scanner is unavailable. Add the item manually or retry the device.";
    public const string SafeScannerUnresolvedMessage =
        "No matching checkout reference was found for that scan.";
    public const string SafeCardReaderUnavailableMessage =
        "Card reader is unavailable. Try another tender or resolve the hardware issue.";
    public const string SafeCardReaderFaultedMessage =
        "Card authorization failed due to a device fault. Retry or use another tender.";
    public const string SafeCardAuthorizationDeclinedMessage =
        "Card was declined. Ask for another payment method or retry with the customer.";
    public const string SafeCardAuthorizationCancelledMessage =
        "Card authorization was cancelled before completion.";
}
