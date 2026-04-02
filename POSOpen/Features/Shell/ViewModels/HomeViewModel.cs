using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Shell;
using POSOpen.Features.Checkout;
using POSOpen.Features.Shell;

namespace POSOpen.Features.Shell.ViewModels;

public sealed partial class HomeViewModel : ObservableObject
{
	private readonly IAppStateService _appStateService;
	private readonly ExecuteManagerOperationUseCase _executeManagerOperationUseCase;
	private string _headline = "POSOpen Mission Control";
	private string _subHeadline = "Operational baseline initialized for role-aware, offline-first workflows.";
	private string _terminalMode = string.Empty;
	private string _syncState = string.Empty;
	private bool _isOffline;
	private string _offlineSinceDisplay = "Online";
	private string _foundationStatus = string.Empty;
	private string _authorizationMessage = string.Empty;

	public HomeViewModel(
		IAppStateService appStateService,
		ExecuteManagerOperationUseCase executeManagerOperationUseCase)
	{
		_appStateService = appStateService;
		_executeManagerOperationUseCase = executeManagerOperationUseCase;
		_appStateService.StateChanged += HandleStateChanged;
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

	public bool IsOffline
	{
		get => _isOffline;
		private set => SetProperty(ref _isOffline, value);
	}

	public string OfflineSinceDisplay
	{
		get => _offlineSinceDisplay;
		private set => SetProperty(ref _offlineSinceDisplay, value);
	}

	public string FoundationStatus
	{
		get => _foundationStatus;
		private set => SetProperty(ref _foundationStatus, value);
	}

	public string AuthorizationMessage
	{
		get => _authorizationMessage;
		private set
		{
			if (SetProperty(ref _authorizationMessage, value))
			{
				OnPropertyChanged(nameof(HasAuthorizationMessage));
			}
		}
	}

	public bool HasAuthorizationMessage => !string.IsNullOrWhiteSpace(AuthorizationMessage);

	public void Refresh()
	{
		TerminalMode = _appStateService.TerminalMode;
		IsOffline = _appStateService.IsOffline;
		SyncState = _appStateService.IsOffline ? "Offline Mode Active" : _appStateService.SyncState;
		OfflineSinceDisplay = _appStateService.IsOffline && _appStateService.OfflineSince is not null
			? _appStateService.OfflineSince.Value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'")
			: "Online";
		FoundationStatus = $"Last updated {_appStateService.LastUpdatedUtc:yyyy-MM-dd HH:mm:ss} UTC";
		AuthorizationMessage = string.Empty;
	}

	private void HandleStateChanged(object? sender, EventArgs e)
	{
		MainThread.BeginInvokeOnMainThread(Refresh);
	}

	public string TerminalModeA11y => $"Terminal mode {TerminalMode}";

	public string SyncStateA11y => $"Sync state {SyncState}";

	[RelayCommand]
	private async Task OpenManagerOperationsAsync()
	{
		var result = _executeManagerOperationUseCase.Execute();
		if (!result.IsSuccess)
		{
			AuthorizationMessage = result.UserMessage;
			return;
		}

		AuthorizationMessage = string.Empty;
		await global::Microsoft.Maui.Controls.Shell.Current.GoToAsync(ShellRoutes.ManagerOperations);
	}

	[RelayCommand]
	private async Task OpenCheckoutAsync()
	{
		await global::Microsoft.Maui.Controls.Shell.Current.GoToAsync(CheckoutRoutes.Cart);
	}
}