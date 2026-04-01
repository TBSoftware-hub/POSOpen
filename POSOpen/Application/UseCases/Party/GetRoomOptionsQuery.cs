using POSOpen.Shared.Operational;

namespace POSOpen.Application.UseCases.Party;

public sealed record GetRoomOptionsQuery(DateTime PartyDateUtc, string SlotId);
