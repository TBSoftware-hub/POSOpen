using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.ApplicationModel;
using POSOpen.Application.Abstractions.Services;

namespace POSOpen.Features.Shell.ViewModels;

public sealed partial class AppShellViewModel : ObservableObject
{
	private readonly IAppStateService _appStateService;

	public AppShellViewModel(IAppStateService appStateService)
	{
		_appStateService = appStateService;
		_appStateService.StateChanged += HandleStateChanged;
		Refresh();
	}

	[ObservableProperty]
	private bool _isOffline;

	[ObservableProperty]
	private string _offlineMessage = "Offline Mode Active - Local operations continue";

	private void HandleStateChanged(object? sender, EventArgs e)
	{
		MainThread.BeginInvokeOnMainThread(Refresh);
	}

	private void Refresh()
	{
		IsOffline = _appStateService.IsOffline;
		if (_appStateService.IsOffline && _appStateService.OfflineSince is not null)
		{
			OfflineMessage = $"Offline Mode Active - Local operations continue (since {_appStateService.OfflineSince:HH:mm} UTC)";
			return;
		}

		OfflineMessage = "Offline Mode Active - Local operations continue";
	}
}
