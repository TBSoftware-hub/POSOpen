namespace POSOpen.Application.UseCases.Checkout;

/// <summary>Input data for receipt printing — safe metadata only, no card data.</summary>
public sealed record ReceiptData(
	Guid TransactionId,
	Guid OperationId,
	long AmountCents,
	string CurrencyCode,
	int ItemCount,
	DateTime TransactionAtUtc);
