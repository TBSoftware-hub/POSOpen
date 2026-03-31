using Microsoft.EntityFrameworkCore;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using System.Collections.Concurrent;
using System.Data;

namespace POSOpen.Infrastructure.Persistence.Repositories;

public sealed class RefundRepository : IRefundRepository
{
	private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> CartLocks = new();
	private readonly IDbContextFactory<PosOpenDbContext> _dbContextFactory;

	public RefundRepository(IDbContextFactory<PosOpenDbContext> dbContextFactory)
	{
		_dbContextFactory = dbContextFactory;
	}

	public async Task<RefundRecord> AddAsync(RefundRecord record, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		dbContext.Set<RefundRecord>().Add(record);
		await dbContext.SaveChangesAsync(ct);
		return record;
	}

	public async Task<RefundRecord> AddAsyncWithBalanceCheckAsync(RefundRecord record, long approvedTotalAmountCents, CancellationToken ct = default)
	{
		var cartLock = CartLocks.GetOrAdd(record.CartSessionId, static _ => new SemaphoreSlim(1, 1));
		await cartLock.WaitAsync(ct);

		try
		{
			await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
			await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

			var currentCompletedSum = await dbContext.Set<RefundRecord>()
				.Where(x => x.CartSessionId == record.CartSessionId && x.Status == RefundStatus.Completed)
				.SumAsync(x => (long?)x.AmountCents, ct);

			var alreadyRefunded = currentCompletedSum ?? 0;
			var remainingBalance = approvedTotalAmountCents - alreadyRefunded;

			if (record.AmountCents > remainingBalance)
			{
				throw new InvalidOperationException(
					$"Refund amount {record.AmountCents} exceeds remaining balance {remainingBalance}.");
			}

			dbContext.Set<RefundRecord>().Add(record);
			await dbContext.SaveChangesAsync(ct);
			await transaction.CommitAsync(ct);
			return record;
		}
		finally
		{
			cartLock.Release();
		}
	}

	public async Task<RefundRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		return await dbContext.Set<RefundRecord>()
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == id, ct);
	}

	public async Task<RefundRecord?> GetByOperationIdAsync(Guid operationId, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		return await dbContext.Set<RefundRecord>()
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.OperationId == operationId, ct);
	}

	public async Task<IReadOnlyList<RefundRecord>> ListByCartSessionAsync(Guid cartSessionId, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		return await dbContext.Set<RefundRecord>()
			.AsNoTracking()
			.Where(x => x.CartSessionId == cartSessionId)
			.OrderByDescending(x => x.CreatedAtUtc)
			.ToListAsync(ct);
	}

	public async Task<long> SumCompletedAmountByCartSessionAsync(Guid cartSessionId, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		var sum = await dbContext.Set<RefundRecord>()
			.AsNoTracking()
			.Where(x => x.CartSessionId == cartSessionId && x.Status == RefundStatus.Completed)
			.SumAsync(x => (long?)x.AmountCents, ct);

		return sum ?? 0;
	}

	public async Task<RefundRecord> UpdateAsync(RefundRecord record, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		dbContext.Set<RefundRecord>().Update(record);
		await dbContext.SaveChangesAsync(ct);
		return record;
	}
}