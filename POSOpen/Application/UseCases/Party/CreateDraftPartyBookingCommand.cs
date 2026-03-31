using POSOpen.Shared.Operational;

namespace POSOpen.Application.UseCases.Party;

public sealed record CreateDraftPartyBookingCommand(
	Guid? BookingId,
	DateTime PartyDateUtc,
	string? SlotId,
	string? PackageId,
	OperationContext Context);
