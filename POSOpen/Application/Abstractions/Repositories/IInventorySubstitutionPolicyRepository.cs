using POSOpen.Domain.Entities;

namespace POSOpen.Application.Abstractions.Repositories;

public interface IInventorySubstitutionPolicyRepository
{
	Task<IReadOnlyList<InventorySubstitutionPolicy>> ListForManagementAsync(CancellationToken ct = default);

	Task<IReadOnlyList<InventorySubstitutionPolicy>> ListActiveForConstrainedOptionsAsync(
		IReadOnlyCollection<string> constrainedOptionIds,
		CancellationToken ct = default);

	Task<InventorySubstitutionPolicy?> GetByIdAsync(Guid policyId, CancellationToken ct = default);

	Task<InventorySubstitutionPolicy?> GetByLastOperationIdAsync(Guid operationId, CancellationToken ct = default);

	Task<InventorySubstitutionPolicy?> FindActiveDuplicateAsync(
		string sourceOptionId,
		string allowedSubstituteOptionId,
		string allowedRolesCsv,
		Guid? excludingPolicyId = null,
		CancellationToken ct = default);

	Task<InventorySubstitutionPolicy> AddAsync(InventorySubstitutionPolicy policy, CancellationToken ct = default);

	Task UpdateAsync(InventorySubstitutionPolicy policy, CancellationToken ct = default);
}
