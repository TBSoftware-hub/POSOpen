namespace POSOpen.Shared.Operational;

public sealed record OperationContext(
	Guid OperationId,
	Guid CorrelationId,
	Guid? CausationId,
	DateTime OccurredUtc);