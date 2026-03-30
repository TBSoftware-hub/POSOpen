using Microsoft.EntityFrameworkCore;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Domain.Entities;

namespace POSOpen.Infrastructure.Persistence.Repositories;

public sealed class FamilyProfileRepository : IFamilyProfileRepository
{
	private readonly IDbContextFactory<PosOpenDbContext> _dbContextFactory;

	public FamilyProfileRepository(IDbContextFactory<PosOpenDbContext> dbContextFactory)
	{
		_dbContextFactory = dbContextFactory;
	}

	public async Task<IReadOnlyList<FamilyProfile>> SearchAsync(string query, CancellationToken ct = default)
	{
		var normalizedQuery = query.Trim().ToLowerInvariant();
		var pattern = $"%{normalizedQuery}%";

		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		return await dbContext.FamilyProfiles
			.Where(profile =>
				EF.Functions.Like(profile.PrimaryContactLastName.ToLower(), pattern) ||
				EF.Functions.Like(profile.PrimaryContactFirstName.ToLower(), pattern) ||
				EF.Functions.Like(profile.Phone.ToLower(), pattern))
			.OrderBy(profile => profile.PrimaryContactLastName)
			.ThenBy(profile => profile.PrimaryContactFirstName)
			.Take(20)
			.ToListAsync(ct);
	}

	public async Task<FamilyProfile?> GetByScanTokenAsync(string token, CancellationToken ct = default)
	{
		var normalizedToken = token.Trim().ToLowerInvariant();
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		return await dbContext.FamilyProfiles.SingleOrDefaultAsync(
			profile => profile.ScanToken != null && profile.ScanToken.ToLower() == normalizedToken,
			ct);
	}

	public async Task<FamilyProfile?> GetByIdAsync(Guid id, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		return await dbContext.FamilyProfiles.SingleOrDefaultAsync(profile => profile.Id == id, ct);
	}

	public async Task AddAsync(FamilyProfile profile, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		dbContext.FamilyProfiles.Add(profile);
		await dbContext.SaveChangesAsync(ct);
	}

	public async Task UpdateAsync(FamilyProfile profile, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		dbContext.FamilyProfiles.Update(profile);
		await dbContext.SaveChangesAsync(ct);
	}
}
