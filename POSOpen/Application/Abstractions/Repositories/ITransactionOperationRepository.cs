using POSOpen.Domain.Entities;

namespace POSOpen.Application.Abstractions.Repositories;

public interface ITransactionOperationRepository
{
	Task<TransactionOperation> AddAsync(TransactionOperation operation, CancellationToken ct = default);

	Task<TransactionOperation?> GetByOperationIdAsync(Guid operationId, CancellationToken ct = default);

	Task<IReadOnlyList<TransactionOperation>> ListByTransactionAsync(
		Guid transactionId,
		CancellationToken ct = default);
}
