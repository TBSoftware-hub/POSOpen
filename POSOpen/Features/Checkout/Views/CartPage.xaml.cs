using POSOpen.Features.Checkout.ViewModels;

namespace POSOpen.Features.Checkout.Views;

public partial class CartPage : ContentPage
{
	public CartPage(CartViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (BindingContext is CartViewModel vm)
			await vm.InitializeCommand.ExecuteAsync(null);
	}
}
