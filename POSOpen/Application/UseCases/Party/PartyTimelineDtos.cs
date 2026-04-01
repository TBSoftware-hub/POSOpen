using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Party;

public sealed record PartyBookingTimelineMilestoneDto(
	string MilestoneKey,
	PartyTimelineMilestoneStatus Status,
	DateTime? EffectiveAtUtc,
	string NextActionCode,
	string NextActionLabel,
	string? RailLabel);

public sealed record PartyBookingTimelineDto(
	Guid BookingId,
	PartyBookingStatus BookingStatus,
	bool IsDepositCommitted,
	DateTime GeneratedAtUtc,
	IReadOnlyList<PartyBookingTimelineMilestoneDto> Milestones,
	DateTime PartyDateUtc = default,
	string SlotId = "");
