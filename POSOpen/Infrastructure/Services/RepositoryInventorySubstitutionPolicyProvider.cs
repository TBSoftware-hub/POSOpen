using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Inventory;
using POSOpen.Domain.Enums;

namespace POSOpen.Infrastructure.Services;

public sealed class RepositoryInventorySubstitutionPolicyProvider : IInventorySubstitutionPolicyProvider
{
	private readonly IInventorySubstitutionPolicyRepository _policyRepository;

	public RepositoryInventorySubstitutionPolicyProvider(IInventorySubstitutionPolicyRepository policyRepository)
	{
		_policyRepository = policyRepository;
	}

	public async Task<IReadOnlyList<InventorySubstitutionPolicyRule>> GetAllowedSubstitutesAsync(
		StaffRole role,
		IReadOnlyCollection<string> constrainedOptionIds,
		CancellationToken ct = default)
	{
		if (constrainedOptionIds.Count == 0)
		{
			return [];
		}

		var policies = await _policyRepository.ListActiveForConstrainedOptionsAsync(constrainedOptionIds, ct);
		var rules = policies
			.Where(x => x.IsActive)
			.Where(x => IsRoleAllowed(x.AllowedRolesCsv, role))
			.OrderBy(x => x.SourceOptionId, StringComparer.Ordinal)
			.ThenBy(x => x.AllowedSubstituteOptionId, StringComparer.Ordinal)
			.Select(x => new InventorySubstitutionPolicyRule(
				x.SourceOptionId,
				x.AllowedSubstituteOptionId,
				ParseRoles(x.AllowedRolesCsv),
				x.IsActive))
			.ToArray();

		return rules;
	}

	private static bool IsRoleAllowed(string rolesCsv, StaffRole role)
	{
		foreach (var value in rolesCsv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
		{
			if (Enum.TryParse<StaffRole>(value, out var parsedRole) && parsedRole == role)
			{
				return true;
			}
		}

		return false;
	}

	private static IReadOnlySet<StaffRole> ParseRoles(string rolesCsv)
	{
		var roles = new HashSet<StaffRole>();
		foreach (var value in rolesCsv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
		{
			if (Enum.TryParse<StaffRole>(value, out var parsedRole))
			{
				roles.Add(parsedRole);
			}
		}

		return roles;
	}
}
