using POSOpen.Domain.Entities;
using POSOpen.Shared.Operational;

namespace POSOpen.Application.Abstractions.Repositories;

public interface IOutboxRepository
{
	Task<OutboxMessage> EnqueueAsync<TPayload>(
		string eventType,
		string aggregateId,
		TPayload payload,
		OperationContext operationContext,
		CancellationToken cancellationToken = default);

	Task<IReadOnlyList<OutboxMessage>> ListPendingAsync(CancellationToken cancellationToken = default);
}