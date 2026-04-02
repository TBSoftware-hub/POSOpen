using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Security;

namespace POSOpen.Application.UseCases.Inventory;

internal static class InventorySubstitutionPolicyAuthorization
{
	public static bool IsManagerAuthorized(
		ICurrentSessionService currentSessionService,
		IAuthorizationPolicyService authorizationPolicyService,
		out CurrentSession? session)
	{
		session = currentSessionService.GetCurrent();
		if (session is null || session.HasStalePermissionSnapshot)
		{
			return false;
		}

		return authorizationPolicyService.HasPermission(session.Role, RolePermissions.ManagerOperationsExecute);
	}
}
