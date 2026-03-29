namespace POSOpen.Domain.Entities;

public sealed class OperationLogEntry
{
	public Guid Id { get; set; }

	public string EventId { get; set; } = string.Empty;

	public string EventType { get; set; } = string.Empty;

	public string AggregateId { get; set; } = string.Empty;

	public Guid OperationId { get; set; }

	public Guid CorrelationId { get; set; }

	public Guid? CausationId { get; set; }

	public int Version { get; set; }

	public string PayloadJson { get; set; } = string.Empty;

	public DateTime OccurredUtc { get; set; }

	public DateTime RecordedUtc { get; set; }
}