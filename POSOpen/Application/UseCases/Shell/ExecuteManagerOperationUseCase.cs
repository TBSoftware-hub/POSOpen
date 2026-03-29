using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Results;
using POSOpen.Application.Security;

namespace POSOpen.Application.UseCases.Shell;

public sealed class ExecuteManagerOperationUseCase
{
	private readonly IAuthorizationPolicyService _authorizationPolicyService;
	private readonly ICurrentSessionService _currentSessionService;

	public ExecuteManagerOperationUseCase(
		IAuthorizationPolicyService authorizationPolicyService,
		ICurrentSessionService currentSessionService)
	{
		_authorizationPolicyService = authorizationPolicyService;
		_currentSessionService = currentSessionService;
	}

	public AppResult<bool> Execute()
	{
		var session = _currentSessionService.GetCurrent();
		if (session is null)
		{
			return AppResult<bool>.Failure("AUTH_FORBIDDEN", "You do not have access to this action.");
		}

		if (session.HasStalePermissionSnapshot)
		{
			return AppResult<bool>.Failure("AUTH_FORBIDDEN", "You do not have access to this action.");
		}

		if (!_authorizationPolicyService.HasPermission(session.Role, RolePermissions.ManagerOperationsExecute))
		{
			return AppResult<bool>.Failure("AUTH_FORBIDDEN", "You do not have access to this action.");
		}

		return AppResult<bool>.Success(true, "Manager operation granted.");
	}
}
