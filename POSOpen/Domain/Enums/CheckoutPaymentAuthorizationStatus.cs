namespace POSOpen.Domain.Enums;

public enum CheckoutPaymentAuthorizationStatus
{
	Approved = 0,
	Declined = 1,
	Unavailable = 2,
	Faulted = 3,
	Cancelled = 4,
}