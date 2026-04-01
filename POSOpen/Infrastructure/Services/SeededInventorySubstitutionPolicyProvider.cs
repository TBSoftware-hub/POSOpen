using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Inventory;
using POSOpen.Domain.Enums;

namespace POSOpen.Infrastructure.Services;

public sealed class SeededInventorySubstitutionPolicyProvider : IInventorySubstitutionPolicyProvider
{
	private static readonly IReadOnlyList<InventorySubstitutionPolicyRule> Rules =
	[
		new("cake-custom", "cake-standard", new HashSet<StaffRole> { StaffRole.Owner, StaffRole.Admin, StaffRole.Manager }, true),
		new("balloon-premium", "balloon-basic", new HashSet<StaffRole> { StaffRole.Owner, StaffRole.Admin, StaffRole.Manager }, true),
		new("banner-custom", "banner-standard", new HashSet<StaffRole> { StaffRole.Owner, StaffRole.Admin, StaffRole.Manager, StaffRole.Cashier }, true),
		new("table-themed", "table-standard", new HashSet<StaffRole> { StaffRole.Owner, StaffRole.Admin, StaffRole.Manager, StaffRole.Cashier }, true),
	];

	public Task<IReadOnlyList<InventorySubstitutionPolicyRule>> GetAllowedSubstitutesAsync(
		StaffRole role,
		IReadOnlyCollection<string> constrainedOptionIds,
		CancellationToken ct = default)
	{
		if (constrainedOptionIds.Count == 0)
		{
			return Task.FromResult<IReadOnlyList<InventorySubstitutionPolicyRule>>([]);
		}

		var filtered = Rules
			.Where(x => x.IsActive)
			.Where(x => constrainedOptionIds.Contains(x.SourceOptionId, StringComparer.Ordinal))
			.Where(x => x.AllowedRoles.Contains(role))
			.OrderBy(x => x.SourceOptionId, StringComparer.Ordinal)
			.ThenBy(x => x.AllowedSubstituteOptionId, StringComparer.Ordinal)
			.ToArray();

		return Task.FromResult<IReadOnlyList<InventorySubstitutionPolicyRule>>(filtered);
	}
}
