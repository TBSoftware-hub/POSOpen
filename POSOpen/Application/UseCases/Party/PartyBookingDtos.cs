using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Party;

public sealed record BookingSlotAvailabilityDto(
	string SlotId,
	bool IsAvailable,
	string? UnavailableReason);

public sealed record BookingAvailabilityDto(
	DateTime PartyDateUtc,
	IReadOnlyList<BookingSlotAvailabilityDto> Slots);

public sealed record PartyBookingDraftDto(
	Guid BookingId,
	DateTime PartyDateUtc,
	string SlotId,
	string PackageId,
	PartyBookingStatus Status,
	Guid OperationId,
	Guid CorrelationId,
	DateTime UpdatedAtUtc);

public sealed record ConfirmPartyBookingResultDto(
	Guid BookingId,
	PartyBookingStatus Status,
	DateTime BookedAtUtc,
	Guid OperationId,
	Guid CorrelationId);
