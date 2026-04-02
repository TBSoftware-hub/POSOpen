using POSOpen.Application.UseCases.Inventory;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.Abstractions.Services;

public interface IInventorySubstitutionPolicyProvider
{
	Task<IReadOnlyList<InventorySubstitutionPolicyRule>> GetAllowedSubstitutesAsync(
		StaffRole role,
		IReadOnlyCollection<string> constrainedOptionIds,
		CancellationToken ct = default);
}
