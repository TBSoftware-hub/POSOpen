using POSOpen.Shared.Operational;

namespace POSOpen.Application.UseCases.Party;

public sealed record MarkPartyBookingCompletedCommand(
	Guid BookingId,
	OperationContext Context);
