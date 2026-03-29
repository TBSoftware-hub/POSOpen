using POSOpen.Features.StaffManagement.ViewModels;

namespace POSOpen.Features.StaffManagement.Views;

public partial class StaffListPage : ContentPage
{
	private readonly StaffListViewModel _viewModel;

	public StaffListPage(StaffListViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.LoadCommand.ExecuteAsync(null);
	}
}
