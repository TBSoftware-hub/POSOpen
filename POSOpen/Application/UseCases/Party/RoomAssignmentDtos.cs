namespace POSOpen.Application.UseCases.Party;

public sealed record RoomOptionItemDto(string RoomId, string DisplayName, bool IsSelectable, string? Reason);

public sealed record RoomOptionsDto(DateTime PartyDateUtc, string SlotId, IReadOnlyList<RoomOptionItemDto> Rooms);

public sealed record RoomAssignmentConflictAlternativesDto(IReadOnlyList<string> AlternativeRooms, IReadOnlyList<string> AlternativeSlots);

public sealed record RoomAssignmentResultDto(
	Guid BookingId,
	string? AssignedRoomId,
	IReadOnlyList<PartyBookingTimelineMilestoneDto>? UpdatedMilestones,
	IReadOnlyList<string>? AlternativeRooms = null,
	IReadOnlyList<string>? AlternativeSlots = null);
