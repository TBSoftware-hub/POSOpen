using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Security;
using POSOpen.Domain.Enums;

namespace POSOpen.Infrastructure.Security;

public sealed class AuthorizationPolicyService : IAuthorizationPolicyService
{
	public bool HasPermission(StaffRole role, string permission)
	{
		if (string.IsNullOrWhiteSpace(permission))
		{
			return false;
		}

		return RolePermissions.HasPermission(role, permission);
	}
}
