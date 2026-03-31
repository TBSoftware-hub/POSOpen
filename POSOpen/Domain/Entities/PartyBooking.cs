using POSOpen.Domain.Enums;

namespace POSOpen.Domain.Entities;

public sealed class PartyBooking
{
	public Guid Id { get; set; }
	public DateTime PartyDateUtc { get; set; }
	public string SlotId { get; set; } = string.Empty;
	public string PackageId { get; set; } = string.Empty;
	public PartyBookingStatus Status { get; set; }
	public Guid OperationId { get; set; }
	public Guid CorrelationId { get; set; }
	public DateTime CreatedAtUtc { get; set; }
	public DateTime UpdatedAtUtc { get; set; }
	public DateTime? BookedAtUtc { get; set; }

	public static PartyBooking CreateDraft(
		Guid id,
		DateTime partyDateUtc,
		string slotId,
		string packageId,
		Guid operationId,
		Guid correlationId,
		DateTime createdAtUtc) =>
		new()
		{
			Id = id,
			PartyDateUtc = partyDateUtc,
			SlotId = slotId,
			PackageId = packageId,
			Status = PartyBookingStatus.Draft,
			OperationId = operationId,
			CorrelationId = correlationId,
			CreatedAtUtc = createdAtUtc,
			UpdatedAtUtc = createdAtUtc,
		};
}
