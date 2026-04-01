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
	public long? DepositAmountCents { get; set; }
	public string? DepositCurrency { get; set; }
	public DateTime? DepositCommittedAtUtc { get; set; }
	public PartyDepositCommitmentStatus DepositCommitmentStatus { get; set; }
	public Guid? DepositCommitmentOperationId { get; set; }
	public DateTime? CompletedAtUtc { get; set; }
	public string? AssignedRoomId { get; set; }
	public DateTime? RoomAssignedAtUtc { get; set; }
	public Guid? RoomAssignmentOperationId { get; set; }

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
			DepositCommitmentStatus = PartyDepositCommitmentStatus.None,
		};

	public void RecordDepositCommitment(long depositAmountCents, string depositCurrency, Guid operationId, Guid correlationId, DateTime committedAtUtc)
	{
		DepositAmountCents = depositAmountCents;
		DepositCurrency = depositCurrency;
		DepositCommittedAtUtc = committedAtUtc;
		DepositCommitmentStatus = PartyDepositCommitmentStatus.Committed;
		DepositCommitmentOperationId = operationId;
		OperationId = operationId;
		CorrelationId = correlationId;
		UpdatedAtUtc = committedAtUtc;
	}

	public void MarkCompleted(Guid operationId, Guid correlationId, DateTime completedAtUtc)
	{
		CompletedAtUtc = completedAtUtc;
		OperationId = operationId;
		CorrelationId = correlationId;
		UpdatedAtUtc = completedAtUtc;
	}

	public void AssignRoom(string roomId, Guid operationId, Guid correlationId, DateTime assignedAtUtc)
	{
		AssignedRoomId = roomId;
		RoomAssignedAtUtc = assignedAtUtc;
		RoomAssignmentOperationId = operationId;
		OperationId = operationId;
		CorrelationId = correlationId;
		UpdatedAtUtc = assignedAtUtc;
	}
}
