using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Domain.Entities;

namespace POSOpen.Infrastructure.Services;

public sealed class OperationIdService : IOperationIdService
{
	private readonly ITransactionOperationRepository _transactionOperationRepository;
	private readonly IUtcClock _clock;

	public OperationIdService(
		ITransactionOperationRepository transactionOperationRepository,
		IUtcClock clock)
	{
		_transactionOperationRepository = transactionOperationRepository;
		_clock = clock;
	}

	public Guid GenerateOperationId() => Guid.NewGuid();

	public async Task SaveOperationAsync(
		Guid operationId,
		Guid transactionId,
		string operationName,
		string? operationData,
		CancellationToken ct = default)
	{
		var operation = TransactionOperation.Create(
			Guid.NewGuid(),
			operationId,
			transactionId,
			operationName,
			operationData,
			"Pending",
			_clock.UtcNow);

		await _transactionOperationRepository.AddAsync(operation, ct);
	}

	public Task<TransactionOperation?> GetOperationAsync(Guid operationId, CancellationToken ct = default)
	{
		return _transactionOperationRepository.GetByOperationIdAsync(operationId, ct);
	}
}
