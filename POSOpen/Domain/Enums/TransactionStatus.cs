namespace POSOpen.Domain.Enums;

public enum TransactionStatus
{
	CompletedOnline = 0,
	CompletedOfflinePendingSync = 1,
	DeferredPayment = 2,
	Error = 3,
}
