using POSOpen.Application.Abstractions.Services;
using POSOpen.Domain.Enums;

namespace POSOpen.Infrastructure.Services;

public sealed class AppStateService : IAppStateService
{
	public bool IsAuthenticated { get; private set; }

	public Guid? CurrentStaffId { get; private set; }

	public StaffRole? CurrentStaffRole { get; private set; }

	public long SessionVersion { get; private set; }

	public long PermissionSnapshotVersion { get; private set; }

	public string TerminalMode { get; private set; } = "Frontline Tablet Ready";

	public string SyncState { get; private set; } = "Queued State Visible";

	public DateTimeOffset LastUpdatedUtc { get; private set; } = DateTimeOffset.UtcNow;

	public void SetCurrentSession(Guid staffId, StaffRole role, long sessionVersion)
	{
		CurrentStaffId = staffId;
		CurrentStaffRole = role;
		SessionVersion = sessionVersion;
		PermissionSnapshotVersion = sessionVersion;
		IsAuthenticated = true;
		LastUpdatedUtc = DateTimeOffset.UtcNow;
	}

	public void SetSessionVersion(long sessionVersion)
	{
		SessionVersion = sessionVersion;
		LastUpdatedUtc = DateTimeOffset.UtcNow;
	}

	public void RefreshPermissionSnapshot()
	{
		PermissionSnapshotVersion = SessionVersion;
		LastUpdatedUtc = DateTimeOffset.UtcNow;
	}

	public void SetTerminalMode(string terminalMode)
	{
		TerminalMode = terminalMode;
		LastUpdatedUtc = DateTimeOffset.UtcNow;
	}

	public void SetSyncState(string syncState)
	{
		SyncState = syncState;
		LastUpdatedUtc = DateTimeOffset.UtcNow;
	}
}