using POSOpen.Features.Security.ViewModels;

namespace POSOpen.Features.Security.Views;

public partial class OverrideApprovalPage : ContentPage
{
	public OverrideApprovalPage(OverrideApprovalViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	private async void OnCancelClicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("..");
	}
}
