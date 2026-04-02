namespace POSOpen.Application.UseCases.Sync;

public sealed record QueueOfflineActionResultDto(
	string MessageId,
	Guid OperationId,
	Guid CorrelationId,
	DateTime EnqueuedUtc,
	long QueueSequence);