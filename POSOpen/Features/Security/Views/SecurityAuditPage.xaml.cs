using POSOpen.Features.Security.ViewModels;

namespace POSOpen.Features.Security.Views;

public partial class SecurityAuditPage : ContentPage
{
	private readonly SecurityAuditViewModel _viewModel;

	public SecurityAuditPage(SecurityAuditViewModel viewModel)
	{
		_viewModel = viewModel;
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.LoadAsync();
	}
}
