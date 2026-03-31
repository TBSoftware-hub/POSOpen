using POSOpen.Domain.Enums;

namespace POSOpen.Domain.Entities;

public sealed class RefundRecord
{
	public Guid Id { get; init; }
	public Guid CartSessionId { get; init; }
	public Guid OperationId { get; init; }
	public RefundStatus Status { get; init; }
	public RefundPath Path { get; init; }
	public long AmountCents { get; init; }
	public string CurrencyCode { get; init; } = "USD";
	public string Reason { get; init; } = string.Empty;
	public Guid ActorStaffId { get; init; }
	public string ActorRole { get; init; } = string.Empty;
	public DateTime CreatedAtUtc { get; init; }
	public DateTime? CompletedAtUtc { get; init; }

	public static RefundRecord Create(
		Guid id,
		Guid cartSessionId,
		Guid operationId,
		RefundStatus status,
		RefundPath path,
		long amountCents,
		string currencyCode,
		string reason,
		Guid actorStaffId,
		string actorRole,
		DateTime createdAtUtc,
		DateTime? completedAtUtc = null) =>
		new()
		{
			Id = id,
			CartSessionId = cartSessionId,
			OperationId = operationId,
			Status = status,
			Path = path,
			AmountCents = amountCents,
			CurrencyCode = currencyCode,
			Reason = reason,
			ActorStaffId = actorStaffId,
			ActorRole = actorRole,
			CreatedAtUtc = createdAtUtc,
			CompletedAtUtc = completedAtUtc,
		};
}