using Microsoft.EntityFrameworkCore;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Domain.Entities;

namespace POSOpen.Infrastructure.Persistence.Repositories;

public sealed class CheckoutPaymentAttemptRepository : ICheckoutPaymentAttemptRepository
{
	private readonly IDbContextFactory<PosOpenDbContext> _dbContextFactory;

	public CheckoutPaymentAttemptRepository(IDbContextFactory<PosOpenDbContext> dbContextFactory)
	{
		_dbContextFactory = dbContextFactory;
	}

	public async Task<CheckoutPaymentAttempt> AddAsync(CheckoutPaymentAttempt attempt, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		dbContext.Set<CheckoutPaymentAttempt>().Add(attempt);
		await dbContext.SaveChangesAsync(ct);
		return attempt;
	}

	public async Task<IReadOnlyList<CheckoutPaymentAttempt>> ListByCartSessionAsync(Guid cartSessionId, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		return await dbContext.Set<CheckoutPaymentAttempt>()
			.Where(x => x.CartSessionId == cartSessionId)
			.OrderByDescending(x => x.OccurredAtUtc)
			.ToListAsync(ct);
	}
}