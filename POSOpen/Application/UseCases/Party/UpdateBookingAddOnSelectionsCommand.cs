using POSOpen.Domain.Enums;
using POSOpen.Shared.Operational;

namespace POSOpen.Application.UseCases.Party;

public sealed record AddOnSelectionItemCommand(string OptionId, PartyAddOnType AddOnType, int Quantity);

public sealed record UpdateBookingAddOnSelectionsCommand(
	Guid BookingId,
	IReadOnlyList<AddOnSelectionItemCommand> Selections,
	OperationContext OperationContext);
