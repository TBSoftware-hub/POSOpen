using POSOpen.Features.StaffManagement.ViewModels;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Features.Authentication;

namespace POSOpen.Features.StaffManagement.Views;

public partial class StaffListPage : ContentPage
{
	private readonly StaffListViewModel _viewModel;
	private readonly IAuthenticationPerformanceTracker _authenticationPerformanceTracker;
	private readonly IUtcClock _clock;

	public StaffListPage(
		StaffListViewModel viewModel,
		IAuthenticationPerformanceTracker authenticationPerformanceTracker,
		IUtcClock clock)
	{
		InitializeComponent();
		_viewModel = viewModel;
		_authenticationPerformanceTracker = authenticationPerformanceTracker;
		_clock = clock;
		BindingContext = _viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.LoadCommand.ExecuteAsync(null);
		Dispatcher.Dispatch(() => _authenticationPerformanceTracker.MarkRoleHomeInteractive("staff/list", _clock.UtcNow));
	}
}
