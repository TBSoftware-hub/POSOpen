using CommunityToolkit.Mvvm.ComponentModel;
using POSOpen.Application.Abstractions.Services;

namespace POSOpen.Features.Shell.ViewModels;

public sealed class HomeViewModel : ObservableObject
{
	private readonly IAppStateService _appStateService;
	private string _headline = "POSOpen Mission Control";
	private string _subHeadline = "Operational baseline initialized for role-aware, offline-first workflows.";
	private string _terminalMode = string.Empty;
	private string _syncState = string.Empty;
	private string _foundationStatus = string.Empty;

	public HomeViewModel(IAppStateService appStateService)
	{
		_appStateService = appStateService;
		Refresh();
	}

	public string Headline
	{
		get => _headline;
		private set => SetProperty(ref _headline, value);
	}

	public string SubHeadline
	{
		get => _subHeadline;
		private set => SetProperty(ref _subHeadline, value);
	}

	public string TerminalMode
	{
		get => _terminalMode;
		private set => SetProperty(ref _terminalMode, value);
	}

	public string SyncState
	{
		get => _syncState;
		private set => SetProperty(ref _syncState, value);
	}

	public string FoundationStatus
	{
		get => _foundationStatus;
		private set => SetProperty(ref _foundationStatus, value);
	}

	public void Refresh()
	{
		TerminalMode = _appStateService.TerminalMode;
		SyncState = _appStateService.SyncState;
		FoundationStatus = $"Last updated {_appStateService.LastUpdatedUtc:yyyy-MM-dd HH:mm:ss} UTC";
	}

	public string TerminalModeA11y => $"Terminal mode {TerminalMode}";

	public string SyncStateA11y => $"Sync state {SyncState}";
}