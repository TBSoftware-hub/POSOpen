using POSOpen.Domain.Entities;
using POSOpen.Shared.Operational;

namespace POSOpen.Application.Abstractions.Repositories;

public interface IOperationLogRepository
{
	Task<OperationLogEntry> AppendAsync<TPayload>(
		string eventType,
		string aggregateId,
		TPayload payload,
		OperationContext operationContext,
		int version = 1,
		CancellationToken cancellationToken = default);

	Task<IReadOnlyList<OperationLogEntry>> ListAsync(CancellationToken cancellationToken = default);

	Task<IReadOnlyList<OperationLogEntry>> ListByEventTypesAsync(
		IReadOnlyList<string> eventTypes,
		CancellationToken cancellationToken = default);
}