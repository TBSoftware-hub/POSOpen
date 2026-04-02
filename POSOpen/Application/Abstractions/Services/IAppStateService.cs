namespace POSOpen.Application.Abstractions.Services;

using POSOpen.Domain.Enums;

public interface IAppStateService
{
	bool IsAuthenticated { get; }

	bool IsOffline { get; }

	DateTimeOffset? OfflineSince { get; }

	Guid? CurrentStaffId { get; }

	StaffRole? CurrentStaffRole { get; }

	long SessionVersion { get; }

	long PermissionSnapshotVersion { get; }

	string TerminalMode { get; }

	string SyncState { get; }

	DateTimeOffset LastUpdatedUtc { get; }

	event EventHandler? StateChanged;

	void SetCurrentSession(Guid staffId, StaffRole role, long sessionVersion);

	void SetSessionVersion(long sessionVersion);

	void RefreshPermissionSnapshot();

	void SetTerminalMode(string terminalMode);

	void SetSyncState(string syncState);

	void SetOfflineMode(bool isOffline);
}