using POSOpen.Shared.Operational;

namespace POSOpen.Application.UseCases.Sync;

public sealed record QueueOfflineActionCommand(
	string EventType,
	string AggregateId,
	Guid ActorStaffId,
	object PayloadSnapshot,
	OperationContext OperationContext);