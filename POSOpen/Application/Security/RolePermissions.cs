namespace POSOpen.Application.Security;

using POSOpen.Domain.Enums;

public static class RolePermissions
{
	public const string StaffRoleAssign = "staff.role.assign";
	public const string ManagerOperationsView = "manager.operations.view";
	public const string ManagerOperationsExecute = "manager.operations.execute";
	public const string StaffManagementView = "staff.management.view";

	private static readonly IReadOnlyDictionary<StaffRole, HashSet<string>> PermissionsByRole =
		new Dictionary<StaffRole, HashSet<string>>
		{
			[StaffRole.Owner] = new(StringComparer.Ordinal)
			{
				StaffRoleAssign,
				ManagerOperationsView,
				ManagerOperationsExecute,
				StaffManagementView
			},
			[StaffRole.Admin] = new(StringComparer.Ordinal)
			{
				StaffRoleAssign,
				ManagerOperationsView,
				ManagerOperationsExecute,
				StaffManagementView
			},
			[StaffRole.Manager] = new(StringComparer.Ordinal)
			{
				ManagerOperationsView,
				ManagerOperationsExecute
			},
			[StaffRole.Cashier] = new(StringComparer.Ordinal)
			{
			}
		};

	public static bool HasPermission(StaffRole role, string permission)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(permission);
		return PermissionsByRole.TryGetValue(role, out var permissions) && permissions.Contains(permission);
	}
}
