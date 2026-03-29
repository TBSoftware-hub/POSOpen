using Microsoft.EntityFrameworkCore;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Infrastructure.Persistence.Repositories;

public sealed class StaffAccountRepository : IStaffAccountRepository
{
	private readonly IDbContextFactory<PosOpenDbContext> _dbContextFactory;

	public StaffAccountRepository(IDbContextFactory<PosOpenDbContext> dbContextFactory)
	{
		_dbContextFactory = dbContextFactory;
	}

	public async Task AddAsync(StaffAccount account, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		dbContext.StaffAccounts.Add(account);
		await dbContext.SaveChangesAsync(ct);
	}

	public async Task<StaffAccount?> GetByIdAsync(Guid id, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		return await dbContext.StaffAccounts.SingleOrDefaultAsync(account => account.Id == id, ct);
	}

	public async Task<StaffAccount?> GetByEmailAsync(string email, CancellationToken ct = default)
	{
		var normalizedEmail = NormalizeEmail(email);
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		return await dbContext.StaffAccounts.SingleOrDefaultAsync(account => account.Email == normalizedEmail, ct);
	}

	public async Task<StaffAccount?> GetByNormalizedEmailForAuthenticationAsync(string email, CancellationToken ct = default)
	{
		var normalizedEmail = NormalizeEmail(email);
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		return await dbContext.StaffAccounts.SingleOrDefaultAsync(account => account.Email == normalizedEmail, ct);
	}

	public async Task<IReadOnlyList<StaffAccount>> ListActiveAsync(CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		return await dbContext.StaffAccounts
			.Where(account => account.Status == StaffAccountStatus.Active)
			.OrderBy(account => account.LastName)
			.ThenBy(account => account.FirstName)
			.ToListAsync(ct);
	}

	public async Task<StaffAccount?> RecordFailedSignInAttemptAsync(
		Guid staffAccountId,
		DateTime occurredUtc,
		int lockoutThreshold,
		TimeSpan lockoutDuration,
		CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		var account = await dbContext.StaffAccounts.SingleOrDefaultAsync(x => x.Id == staffAccountId, ct);
		if (account is null)
		{
			return null;
		}

		account.FailedLoginAttempts += 1;
		if (account.FailedLoginAttempts >= lockoutThreshold)
		{
			account.LockedUntilUtc = occurredUtc.Add(lockoutDuration);
		}

		account.UpdatedAtUtc = occurredUtc;
		await dbContext.SaveChangesAsync(ct);
		return account;
	}

	public async Task<StaffAccount?> RecordSuccessfulSignInAsync(Guid staffAccountId, DateTime occurredUtc, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		var account = await dbContext.StaffAccounts.SingleOrDefaultAsync(x => x.Id == staffAccountId, ct);
		if (account is null)
		{
			return null;
		}

		account.FailedLoginAttempts = 0;
		account.LockedUntilUtc = null;
		account.UpdatedAtUtc = occurredUtc;
		await dbContext.SaveChangesAsync(ct);
		return account;
	}

	public async Task UpdateAsync(StaffAccount account, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		dbContext.StaffAccounts.Update(account);
		await dbContext.SaveChangesAsync(ct);
	}

	private static string NormalizeEmail(string email)
	{
		return email.Trim().ToLowerInvariant();
	}
}
