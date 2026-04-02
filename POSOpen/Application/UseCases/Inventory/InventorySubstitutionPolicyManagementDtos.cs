using POSOpen.Domain.Enums;
using POSOpen.Shared.Operational;

namespace POSOpen.Application.UseCases.Inventory;

public sealed record InventorySubstitutionPolicyManagementDto(
	Guid PolicyId,
	string SourceOptionId,
	string SourceDisplayName,
	string AllowedSubstituteOptionId,
	string AllowedSubstituteDisplayName,
	IReadOnlyList<StaffRole> AllowedRoles,
	bool IsActive,
	DateTime UpdatedAtUtc,
	Guid UpdatedByStaffId)
{
	public string AllowedRolesDisplay => string.Join(", ", AllowedRoles.Select(static role => role.ToString()));
}

public sealed record GetInventorySubstitutionPoliciesQuery(
	OperationContext Context);

public sealed record CreateInventorySubstitutionPolicyCommand(
	string SourceOptionId,
	string AllowedSubstituteOptionId,
	IReadOnlyList<StaffRole> AllowedRoles,
	bool IsActive,
	OperationContext Context);

public sealed record UpdateInventorySubstitutionPolicyCommand(
	Guid PolicyId,
	string SourceOptionId,
	string AllowedSubstituteOptionId,
	IReadOnlyList<StaffRole> AllowedRoles,
	bool IsActive,
	OperationContext Context);

public sealed record DeleteInventorySubstitutionPolicyCommand(
	Guid PolicyId,
	OperationContext Context);
