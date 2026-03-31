using POSOpen.Domain.Enums;

namespace POSOpen.Domain.Entities;

public sealed class ReceiptMetadata
{
	public Guid Id { get; init; }
	public Guid OperationId { get; init; }

	/// <summary>Cart session ID — serves as the transaction identifier for V1.</summary>
	public Guid TransactionId { get; init; }

	public long AmountCents { get; init; }
	public string CurrencyCode { get; init; } = "USD";
	public int ItemCount { get; init; }
	public DateTime? PrintedAtUtc { get; init; }
	public PrintStatus PrintStatus { get; init; }
	public string? DiagnosticCode { get; init; }
	public DateTime CreatedAtUtc { get; init; }

	public static ReceiptMetadata Create(
		Guid id,
		Guid operationId,
		Guid transactionId,
		long amountCents,
		string currencyCode,
		int itemCount,
		DateTime? printedAtUtc,
		PrintStatus printStatus,
		string? diagnosticCode,
		DateTime createdAtUtc) =>
		new()
		{
			Id = id,
			OperationId = operationId,
			TransactionId = transactionId,
			AmountCents = amountCents,
			CurrencyCode = currencyCode,
			ItemCount = itemCount,
			PrintedAtUtc = printedAtUtc,
			PrintStatus = printStatus,
			DiagnosticCode = diagnosticCode,
			CreatedAtUtc = createdAtUtc,
		};
}
