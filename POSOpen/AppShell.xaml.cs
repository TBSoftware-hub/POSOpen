using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Security;
using POSOpen.Features.Authentication;

namespace POSOpen;

public partial class AppShell : Shell
{
	private readonly IAuthorizationPolicyService _authorizationPolicyService;
	private readonly ICurrentSessionService _currentSessionService;
	private readonly IAppStateService _appStateService;

	public AppShell(
		IAuthorizationPolicyService authorizationPolicyService,
		ICurrentSessionService currentSessionService,
		IAppStateService appStateService)
	{
		_authorizationPolicyService = authorizationPolicyService;
		_currentSessionService = currentSessionService;
		_appStateService = appStateService;
		InitializeComponent();
		Loaded += OnLoaded;
		ApplyRoleAwareVisibility();
	}

	private async void OnLoaded(object? sender, EventArgs e)
	{
		if (!_appStateService.IsAuthenticated)
		{
			await GoToAsync($"//{AuthenticationRoutes.SignIn}");
		}
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
			SignInShellContent.IsVisible = true;
			HomeShellContent.IsVisible = false;
			StaffShellContent.IsVisible = false;
			ManagerShellContent.IsVisible = false;
			SecurityAuditShellContent.IsVisible = false;
			return;
		}

		SignInShellContent.IsVisible = false;
		HomeShellContent.IsVisible = true;
		StaffShellContent.IsVisible = _authorizationPolicyService.HasPermission(session.Role, RolePermissions.StaffManagementView);
		ManagerShellContent.IsVisible = _authorizationPolicyService.HasPermission(session.Role, RolePermissions.ManagerOperationsView);
		SecurityAuditShellContent.IsVisible = _authorizationPolicyService.HasPermission(session.Role, RolePermissions.SecurityAuditRead);
	}
}
