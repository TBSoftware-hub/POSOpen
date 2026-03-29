using POSOpen.Features.Authentication.ViewModels;

namespace POSOpen.Features.Authentication.Views;

public partial class SignInPage : ContentPage
{
	public SignInPage(SignInViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
