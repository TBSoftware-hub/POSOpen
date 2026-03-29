using FluentAssertions;
using POSOpen.Application.Security;
using POSOpen.Domain.Enums;
using POSOpen.Infrastructure.Security;

namespace POSOpen.Tests.Unit.Security;

public sealed class AuthorizationPolicyServiceTests
{
	[Theory]
	[InlineData(StaffRole.Owner, RolePermissions.StaffRoleAssign, true)]
	[InlineData(StaffRole.Admin, RolePermissions.StaffRoleAssign, true)]
	[InlineData(StaffRole.Manager, RolePermissions.StaffRoleAssign, false)]
	[InlineData(StaffRole.Cashier, RolePermissions.StaffRoleAssign, false)]
	[InlineData(StaffRole.Owner, RolePermissions.ManagerOperationsExecute, true)]
	[InlineData(StaffRole.Admin, RolePermissions.ManagerOperationsExecute, true)]
	[InlineData(StaffRole.Manager, RolePermissions.ManagerOperationsExecute, true)]
	[InlineData(StaffRole.Cashier, RolePermissions.ManagerOperationsExecute, false)]
	public void HasPermission_matches_expected_matrix(StaffRole role, string permission, bool expected)
	{
		var service = new AuthorizationPolicyService();

		service.HasPermission(role, permission).Should().Be(expected);
	}

	[Fact]
	public void HasPermission_denies_unknown_permission_by_default()
	{
		var service = new AuthorizationPolicyService();

		service.HasPermission(StaffRole.Owner, "unknown.permission").Should().BeFalse();
	}
}
