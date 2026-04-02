using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Inventory;

public sealed record InventorySubstitutionPolicyRule(
	string SourceOptionId,
	string AllowedSubstituteOptionId,
	IReadOnlySet<StaffRole> AllowedRoles,
	bool IsActive);

public sealed record AllowedSubstituteOptionDto(
	string SourceOptionId,
	string AllowedSubstituteOptionId,
	string DisplayName);
