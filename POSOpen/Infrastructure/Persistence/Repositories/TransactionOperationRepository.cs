using Microsoft.EntityFrameworkCore;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Domain.Entities;

namespace POSOpen.Infrastructure.Persistence.Repositories;

public sealed class TransactionOperationRepository : ITransactionOperationRepository
{
	private readonly IDbContextFactory<PosOpenDbContext> _dbContextFactory;

	public TransactionOperationRepository(IDbContextFactory<PosOpenDbContext> dbContextFactory)
	{
		_dbContextFactory = dbContextFactory;
	}

	public async Task<TransactionOperation> AddAsync(TransactionOperation operation, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		dbContext.Set<TransactionOperation>().Add(operation);
		await dbContext.SaveChangesAsync(ct);
		return operation;
	}

	public async Task<TransactionOperation?> GetByOperationIdAsync(Guid operationId, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		return await dbContext.Set<TransactionOperation>()
			.FirstOrDefaultAsync(x => x.OperationId == operationId, ct);
	}

	public async Task<IReadOnlyList<TransactionOperation>> ListByTransactionAsync(Guid transactionId, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		return await dbContext.Set<TransactionOperation>()
			.Where(x => x.TransactionId == transactionId)
			.OrderByDescending(x => x.CreatedAtUtc)
			.ToListAsync(ct);
	}
}
