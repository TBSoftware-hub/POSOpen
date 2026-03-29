using POSOpen.Features.StaffManagement.ViewModels;

namespace POSOpen.Features.StaffManagement.Views;

public partial class CreateStaffAccountPage : ContentPage
{
	private readonly CreateStaffAccountViewModel _viewModel;

	public CreateStaffAccountPage(CreateStaffAccountViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
	}

	private void OnEmailUnfocused(object? sender, FocusEventArgs e)
	{
		_viewModel.ValidateEmailCommand.Execute(null);
	}
}
