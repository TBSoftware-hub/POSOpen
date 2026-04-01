using POSOpen.Shared.Operational;

namespace POSOpen.Application.UseCases.Party;

public sealed record RecordPartyDepositCommitmentCommand(
	Guid BookingId,
	long DepositAmountCents,
	string? DepositCurrency,
	OperationContext Context);
