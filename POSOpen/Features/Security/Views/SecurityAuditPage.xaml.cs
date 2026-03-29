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
		try
		{
			await _viewModel.LoadAsync();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"SecurityAuditPage OnAppearing failed: {ex}");
		}
	}
}
