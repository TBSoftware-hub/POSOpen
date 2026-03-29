using POSOpen.Features.Shell.ViewModels;

namespace POSOpen.Features.Shell.Views;

public partial class HomePage : ContentPage
{
	public HomePage(HomeViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}