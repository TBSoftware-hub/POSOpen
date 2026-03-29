namespace POSOpen.Application.Abstractions.Security;

using POSOpen.Domain.Enums;

public interface IAuthorizationPolicyService
{
	bool HasPermission(StaffRole role, string permission);
}
