using POSOpen.Domain.Entities;

namespace POSOpen.Application.Abstractions.Repositories;

public interface IReceiptMetadataRepository
{
	Task<ReceiptMetadata> AddAsync(ReceiptMetadata receipt, CancellationToken ct = default);

	Task<ReceiptMetadata?> GetByOperationIdAsync(Guid operationId, CancellationToken ct = default);

	Task<IReadOnlyList<ReceiptMetadata>> ListByTransactionAsync(Guid transactionId, CancellationToken ct = default);
}
