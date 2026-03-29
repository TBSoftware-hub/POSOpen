namespace POSOpen.Application.UseCases.Security;

public sealed record SecurityAuditRecordDto(
	Guid Id,
	string EventType,
	string AggregateId,
	Guid OperationId,
	Guid CorrelationId,
	DateTime OccurredUtc,
	DateTime RecordedUtc,
	string PayloadJson);
