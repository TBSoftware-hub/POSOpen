namespace POSOpen.Application.UseCases.Inventory;

public static class InventorySubstitutionPolicyConstants
{
	public const string ErrorAuthForbidden = "AUTH_FORBIDDEN";
	public const string ErrorPolicyNotFound = "INVENTORY_SUB_POLICY_NOT_FOUND";
	public const string ErrorSourceRequired = "INVENTORY_SUB_POLICY_SOURCE_REQUIRED";
	public const string ErrorSubstituteRequired = "INVENTORY_SUB_POLICY_SUBSTITUTE_REQUIRED";
	public const string ErrorRoleRequired = "INVENTORY_SUB_POLICY_ROLE_REQUIRED";
	public const string ErrorSourceInvalid = "INVENTORY_SUB_POLICY_SOURCE_INVALID";
	public const string ErrorSubstituteInvalid = "INVENTORY_SUB_POLICY_SUBSTITUTE_INVALID";
	public const string ErrorSelfReference = "INVENTORY_SUB_POLICY_SELF_REFERENCE";
	public const string ErrorDuplicate = "INVENTORY_SUB_POLICY_DUPLICATE";

	public const string SafeAuthForbiddenMessage = "You do not have access to this action.";
	public const string SafePolicyNotFoundMessage = "The substitution policy could not be found.";
	public const string SafeSourceRequiredMessage = "Select a source item to continue.";
	public const string SafeSubstituteRequiredMessage = "Select a substitute item to continue.";
	public const string SafeRoleRequiredMessage = "Select at least one allowed role.";
	public const string SafeSourceInvalidMessage = "The selected source item is not in the inventory catalog.";
	public const string SafeSubstituteInvalidMessage = "The selected substitute item is not in the inventory catalog.";
	public const string SafeSelfReferenceMessage = "Source and substitute items must be different.";
	public const string SafeDuplicateMessage = "An active policy with the same source, substitute, and roles already exists.";

	public const string ListLoadedMessage = "Substitution policies loaded.";
	public const string CreatedMessage = "Substitution policy created.";
	public const string UpdatedMessage = "Substitution policy updated.";
	public const string UpdateIdempotentMessage = "Substitution policy update already applied.";
	public const string DeletedMessage = "Substitution policy deleted.";
	public const string DeleteIdempotentMessage = "Substitution policy delete already applied.";
	public const string AlreadyInactiveMessage = "Substitution policy is already inactive.";
}
