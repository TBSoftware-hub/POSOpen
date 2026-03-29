using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Security;

namespace POSOpen;

public partial class AppShell : Shell
{
	private readonly IAuthorizationPolicyService _authorizationPolicyService;
	private readonly ICurrentSessionService _currentSessionService;

	public AppShell(
		IAuthorizationPolicyService authorizationPolicyService,
		ICurrentSessionService currentSessionService)
	{
		_authorizationPolicyService = authorizationPolicyService;
		_currentSessionService = currentSessionService;
		InitializeComponent();
		ApplyRoleAwareVisibility();
	}

	protected override void OnNavigated(ShellNavigatedEventArgs args)
	{
		base.OnNavigated(args);
		ApplyRoleAwareVisibility();
	}

	private void ApplyRoleAwareVisibility()
	{
		var session = _currentSessionService.GetCurrent();
		if (session is null || session.HasStalePermissionSnapshot)
		{
			StaffShellContent.IsVisible = false;
			ManagerShellContent.IsVisible = false;
			return;
		}

		StaffShellContent.IsVisible = _authorizationPolicyService.HasPermission(session.Role, RolePermissions.StaffManagementView);
		ManagerShellContent.IsVisible = _authorizationPolicyService.HasPermission(session.Role, RolePermissions.ManagerOperationsView);
	}
}
