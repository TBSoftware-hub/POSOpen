namespace POSOpen.Domain.Entities;

public sealed class TransactionOperation
{
	public Guid Id { get; init; }

	/// <summary>Idempotency key — unique per logical operation for offline replay deduplication.</summary>
	public Guid OperationId { get; init; }

	/// <summary>Cart session ID — transaction identifier for V1.</summary>
	public string TransactionId { get; init; } = string.Empty;

	public string OperationName { get; init; } = string.Empty;

	/// <summary>JSON-serialized operation payload for replay context. Must not contain PCI data.</summary>
	public string? OperationData { get; init; }

	public string Status { get; init; } = "Pending";
	public DateTime CreatedAtUtc { get; init; }
	public DateTime? CompletedAtUtc { get; init; }

	public static TransactionOperation Create(
		Guid id,
		Guid operationId,
		string transactionId,
		string operationName,
		string? operationData,
		string status,
		DateTime createdAtUtc,
		DateTime? completedAtUtc = null) =>
		new()
		{
			Id = id,
			OperationId = operationId,
			TransactionId = transactionId,
			OperationName = operationName,
			OperationData = operationData,
			Status = status,
			CreatedAtUtc = createdAtUtc,
			CompletedAtUtc = completedAtUtc,
		};
}
