using Microsoft.EntityFrameworkCore;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Domain.Entities;

namespace POSOpen.Infrastructure.Persistence.Repositories;

public sealed class InventorySubstitutionPolicyRepository : IInventorySubstitutionPolicyRepository
{
	private readonly IDbContextFactory<PosOpenDbContext> _dbContextFactory;

	public InventorySubstitutionPolicyRepository(IDbContextFactory<PosOpenDbContext> dbContextFactory)
	{
		_dbContextFactory = dbContextFactory;
	}

	public async Task<IReadOnlyList<InventorySubstitutionPolicy>> ListForManagementAsync(CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		return await dbContext.Set<InventorySubstitutionPolicy>()
			.AsNoTracking()
			.OrderBy(x => x.SourceOptionId)
			.ThenBy(x => x.AllowedSubstituteOptionId)
			.ThenBy(x => x.Id)
			.ToListAsync(ct);
	}

	public async Task<IReadOnlyList<InventorySubstitutionPolicy>> ListActiveForConstrainedOptionsAsync(
		IReadOnlyCollection<string> constrainedOptionIds,
		CancellationToken ct = default)
	{
		if (constrainedOptionIds.Count == 0)
		{
			return [];
		}

		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		return await dbContext.Set<InventorySubstitutionPolicy>()
			.AsNoTracking()
			.Where(x => x.IsActive)
			.Where(x => constrainedOptionIds.Contains(x.SourceOptionId))
			.OrderBy(x => x.SourceOptionId)
			.ThenBy(x => x.AllowedSubstituteOptionId)
			.ThenBy(x => x.Id)
			.ToListAsync(ct);
	}

	public async Task<InventorySubstitutionPolicy?> GetByIdAsync(Guid policyId, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		return await dbContext.Set<InventorySubstitutionPolicy>()
			.FirstOrDefaultAsync(x => x.Id == policyId, ct);
	}

	public async Task<InventorySubstitutionPolicy?> GetByLastOperationIdAsync(Guid operationId, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		return await dbContext.Set<InventorySubstitutionPolicy>()
			.FirstOrDefaultAsync(x => x.LastOperationId == operationId, ct);
	}

	public async Task<InventorySubstitutionPolicy?> FindActiveDuplicateAsync(
		string sourceOptionId,
		string allowedSubstituteOptionId,
		string allowedRolesCsv,
		Guid? excludingPolicyId = null,
		CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		var query = dbContext.Set<InventorySubstitutionPolicy>()
			.AsNoTracking()
			.Where(x => x.IsActive)
			.Where(x => x.SourceOptionId == sourceOptionId)
			.Where(x => x.AllowedSubstituteOptionId == allowedSubstituteOptionId)
			.Where(x => x.AllowedRolesCsv == allowedRolesCsv);

		if (excludingPolicyId.HasValue)
		{
			query = query.Where(x => x.Id != excludingPolicyId.Value);
		}

		return await query.FirstOrDefaultAsync(ct);
	}

	public async Task<InventorySubstitutionPolicy> AddAsync(InventorySubstitutionPolicy policy, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		dbContext.Set<InventorySubstitutionPolicy>().Add(policy);
		await dbContext.SaveChangesAsync(ct);
		return policy;
	}

	public async Task UpdateAsync(InventorySubstitutionPolicy policy, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		dbContext.Set<InventorySubstitutionPolicy>().Update(policy);
		await dbContext.SaveChangesAsync(ct);
	}
}
