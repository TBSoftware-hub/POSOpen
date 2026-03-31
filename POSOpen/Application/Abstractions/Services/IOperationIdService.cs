using POSOpen.Domain.Entities;

namespace POSOpen.Application.Abstractions.Services;

public interface IOperationIdService
{
	Guid GenerateOperationId();

	Task SaveOperationAsync(
		Guid operationId,
		string operationName,
		string? operationData,
		CancellationToken ct = default);

	Task<TransactionOperation?> GetOperationAsync(Guid operationId, CancellationToken ct = default);
}
