using POSOpen.Features.Shell.ViewModels;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Features.Authentication;

namespace POSOpen.Features.Shell.Views;

public partial class HomePage : ContentPage
{
	private readonly IAuthenticationPerformanceTracker _authenticationPerformanceTracker;
	private readonly IUtcClock _clock;

	public HomePage(
		HomeViewModel viewModel,
		IAuthenticationPerformanceTracker authenticationPerformanceTracker,
		IUtcClock clock)
	{
		InitializeComponent();
		_authenticationPerformanceTracker = authenticationPerformanceTracker;
		_clock = clock;
		BindingContext = viewModel;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		Dispatcher.Dispatch(() => _authenticationPerformanceTracker.MarkRoleHomeInteractive("home", _clock.UtcNow));
	}
}