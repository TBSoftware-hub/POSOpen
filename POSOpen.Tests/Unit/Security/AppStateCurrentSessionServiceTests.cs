using FluentAssertions;
using POSOpen.Domain.Enums;
using POSOpen.Infrastructure.Services;

namespace POSOpen.Tests.Unit.Security;

public sealed class AppStateCurrentSessionServiceTests
{
	[Fact]
	public void GetCurrent_reflects_stale_permission_snapshot_until_refresh()
	{
		var appState = new AppStateService();
		var sessionService = new AppStateCurrentSessionService(appState);

		appState.SetCurrentSession(Guid.NewGuid(), StaffRole.Admin, 1);
		var initial = sessionService.GetCurrent();
		initial.Should().NotBeNull();
		initial!.HasStalePermissionSnapshot.Should().BeFalse();

		sessionService.IncrementSessionVersion();
		var stale = sessionService.GetCurrent();
		stale.Should().NotBeNull();
		stale!.HasStalePermissionSnapshot.Should().BeTrue();

		sessionService.RefreshPermissionSnapshot();
		var refreshed = sessionService.GetCurrent();
		refreshed.Should().NotBeNull();
		refreshed!.HasStalePermissionSnapshot.Should().BeFalse();
	}
}
