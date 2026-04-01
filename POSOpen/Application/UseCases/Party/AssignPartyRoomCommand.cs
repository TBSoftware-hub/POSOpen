using POSOpen.Shared.Operational;

namespace POSOpen.Application.UseCases.Party;

public sealed record AssignPartyRoomCommand(Guid BookingId, string RoomId, OperationContext Context);
