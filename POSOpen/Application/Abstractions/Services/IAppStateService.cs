namespace POSOpen.Application.Abstractions.Services;

public interface IAppStateService
{
	string TerminalMode { get; }

	string SyncState { get; }

	DateTimeOffset LastUpdatedUtc { get; }

	void SetTerminalMode(string terminalMode);

	void SetSyncState(string syncState);
}