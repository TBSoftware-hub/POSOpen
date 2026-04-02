using FluentAssertions;
using POSOpen.Infrastructure.Services;

namespace POSOpen.Tests.Unit.Sync;

public sealed class AppStateServiceOfflineModeTests
{
	[Fact]
	public void SetOfflineMode_true_sets_offline_and_timestamp()
	{
		var appState = new AppStateService();

		appState.SetOfflineMode(true);

		appState.IsOffline.Should().BeTrue();
		appState.OfflineSince.Should().NotBeNull();
	}

	[Fact]
	public void SetOfflineMode_false_clears_offline_and_timestamp()
	{
		var appState = new AppStateService();
		appState.SetOfflineMode(true);

		appState.SetOfflineMode(false);

		appState.IsOffline.Should().BeFalse();
		appState.OfflineSince.Should().BeNull();
	}

	[Fact]
	public void SetOfflineMode_true_twice_keeps_original_timestamp()
	{
		var appState = new AppStateService();
		appState.SetOfflineMode(true);
		var initialTimestamp = appState.OfflineSince;

		appState.SetOfflineMode(true);

		appState.OfflineSince.Should().Be(initialTimestamp);
	}

	[Fact]
	public void Mutating_methods_raise_state_changed_once_each()
	{
		var appState = new AppStateService();
		var raiseCount = 0;
		appState.StateChanged += (_, _) => raiseCount++;
		var staffId = Guid.NewGuid();

		appState.SetCurrentSession(staffId, Domain.Enums.StaffRole.Admin, 1);
		appState.SetSessionVersion(2);
		appState.RefreshPermissionSnapshot();
		appState.SetTerminalMode("Tablet");
		appState.SetSyncState("Offline");
		appState.SetOfflineMode(true);

		raiseCount.Should().Be(6);
	}
}
