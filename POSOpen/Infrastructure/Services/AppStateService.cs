using POSOpen.Application.Abstractions.Services;

namespace POSOpen.Infrastructure.Services;

public sealed class AppStateService : IAppStateService
{
	public string TerminalMode { get; private set; } = "Frontline Tablet Ready";

	public string SyncState { get; private set; } = "Queued State Visible";

	public DateTimeOffset LastUpdatedUtc { get; private set; } = DateTimeOffset.UtcNow;

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