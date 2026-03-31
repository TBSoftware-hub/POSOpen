using POSOpen.Features.Checkout.ViewModels;

namespace POSOpen.Features.Checkout.Views;

[QueryProperty(nameof(CartSessionIdParam), "cartSessionId")]
public partial class CheckoutCompletionPage : ContentPage
{
	public CheckoutCompletionPage(CheckoutCompletionViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	public string CartSessionIdParam
	{
		set
		{
			if (BindingContext is CheckoutCompletionViewModel viewModel)
			{
				viewModel.CartSessionIdParam = value;
			}
		}
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (BindingContext is CheckoutCompletionViewModel vm)
		{
			await vm.InitializeCommand.ExecuteAsync(null);
		}
	}
}
