using Microsoft.EntityFrameworkCore;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Domain.Entities;

namespace POSOpen.Infrastructure.Persistence.Repositories;

public sealed class ReceiptMetadataRepository : IReceiptMetadataRepository
{
	private readonly IDbContextFactory<PosOpenDbContext> _dbContextFactory;

	public ReceiptMetadataRepository(IDbContextFactory<PosOpenDbContext> dbContextFactory)
	{
		_dbContextFactory = dbContextFactory;
	}

	public async Task<ReceiptMetadata> AddAsync(ReceiptMetadata receipt, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		dbContext.Set<ReceiptMetadata>().Add(receipt);
		await dbContext.SaveChangesAsync(ct);
		return receipt;
	}

	public async Task<ReceiptMetadata?> GetByOperationIdAsync(Guid operationId, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		return await dbContext.Set<ReceiptMetadata>()
			.FirstOrDefaultAsync(x => x.OperationId == operationId, ct);
	}

	public async Task<IReadOnlyList<ReceiptMetadata>> ListByTransactionAsync(Guid transactionId, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		return await dbContext.Set<ReceiptMetadata>()
			.Where(x => x.TransactionId == transactionId)
			.OrderByDescending(x => x.CreatedAtUtc)
			.ToListAsync(ct);
	}
}
