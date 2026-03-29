using POSOpen.Application.UseCases.Shell;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Features.Authentication;
using POSOpen.Features.Shell.ViewModels;

namespace POSOpen.Features.Shell.Views;

public partial class ManagerOperationsPage : ContentPage
{
	private readonly ExecuteManagerOperationUseCase _executeManagerOperationUseCase;
	private readonly IAuthenticationPerformanceTracker _authenticationPerformanceTracker;
	private readonly IUtcClock _clock;

	public ManagerOperationsPage(
		ManagerOperationsViewModel viewModel,
		ExecuteManagerOperationUseCase executeManagerOperationUseCase,
		IAuthenticationPerformanceTracker authenticationPerformanceTracker,
		IUtcClock clock)
	{
		_executeManagerOperationUseCase = executeManagerOperationUseCase;
		_authenticationPerformanceTracker = authenticationPerformanceTracker;
		_clock = clock;
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		var result = _executeManagerOperationUseCase.Execute();
		if (result.IsSuccess)
		{
			Dispatcher.Dispatch(() => _authenticationPerformanceTracker.MarkRoleHomeInteractive("manager/operations", _clock.UtcNow));
			return;
		}

		await global::Microsoft.Maui.Controls.Shell.Current.DisplayAlertAsync("Access denied", result.UserMessage, "OK");
		await global::Microsoft.Maui.Controls.Shell.Current.GoToAsync($"//{ShellRoutes.Home}");
	}
}
