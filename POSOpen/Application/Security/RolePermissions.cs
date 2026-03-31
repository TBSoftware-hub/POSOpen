namespace POSOpen.Application.Security;

using POSOpen.Domain.Enums;

public static class RolePermissions
{
	public const string AdmissionsLookup = "admissions.lookup";
	public const string StaffRoleAssign = "staff.role.assign";
	public const string ManagerOperationsView = "manager.operations.view";
	public const string ManagerOperationsExecute = "manager.operations.execute";
	public const string StaffManagementView = "staff.management.view";
	public const string SecurityOverrideExecute = "security.override.execute";
	public const string SecurityAuditRead = "security.audit.read";
	public const string CheckoutRefundInitiate = "checkout.refund.initiate";
	public const string CheckoutRefundApprove = "checkout.refund.approve";

	private static readonly IReadOnlyDictionary<StaffRole, HashSet<string>> PermissionsByRole =
		new Dictionary<StaffRole, HashSet<string>>
		{
			[StaffRole.Owner] = new(StringComparer.Ordinal)
			{
				AdmissionsLookup,
				StaffRoleAssign,
				ManagerOperationsView,
				ManagerOperationsExecute,
				StaffManagementView,
				SecurityOverrideExecute,
				SecurityAuditRead,
				CheckoutRefundInitiate,
				CheckoutRefundApprove
			},
			[StaffRole.Admin] = new(StringComparer.Ordinal)
			{
				AdmissionsLookup,
				StaffRoleAssign,
				ManagerOperationsView,
				ManagerOperationsExecute,
				StaffManagementView,
				SecurityOverrideExecute,
				SecurityAuditRead,
				CheckoutRefundInitiate,
				CheckoutRefundApprove
			},
			[StaffRole.Manager] = new(StringComparer.Ordinal)
			{
				AdmissionsLookup,
				ManagerOperationsView,
				ManagerOperationsExecute,
				SecurityOverrideExecute,
				CheckoutRefundInitiate,
				CheckoutRefundApprove
			},
			[StaffRole.Cashier] = new(StringComparer.Ordinal)
			{
				AdmissionsLookup,
				CheckoutRefundInitiate
			}
		};

	public static bool HasPermission(StaffRole role, string permission)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(permission);
		return PermissionsByRole.TryGetValue(role, out var permissions) && permissions.Contains(permission);
	}
}
