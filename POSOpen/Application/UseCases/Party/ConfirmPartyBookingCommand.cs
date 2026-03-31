using POSOpen.Shared.Operational;

namespace POSOpen.Application.UseCases.Party;

public sealed record ConfirmPartyBookingCommand(
	Guid BookingId,
	OperationContext Context);
