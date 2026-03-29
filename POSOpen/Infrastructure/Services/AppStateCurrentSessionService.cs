using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Security;

namespace POSOpen.Infrastructure.Services;

public sealed class AppStateCurrentSessionService : ICurrentSessionService
{
	private readonly IAppStateService _appStateService;

	public AppStateCurrentSessionService(IAppStateService appStateService)
	{
		_appStateService = appStateService;
	}

	public CurrentSession? GetCurrent()
	{
		if (!_appStateService.IsAuthenticated ||
			!_appStateService.CurrentStaffId.HasValue ||
			!_appStateService.CurrentStaffRole.HasValue)
		{
			return null;
		}

		return new CurrentSession(
			_appStateService.CurrentStaffId.Value,
			_appStateService.CurrentStaffRole.Value,
			_appStateService.SessionVersion,
			_appStateService.PermissionSnapshotVersion);
	}

	public void RefreshPermissionSnapshot()
	{
		_appStateService.RefreshPermissionSnapshot();
	}

	public long IncrementSessionVersion()
	{
		var nextVersion = _appStateService.SessionVersion + 1;
		_appStateService.SetSessionVersion(nextVersion);
		return nextVersion;
	}
}
