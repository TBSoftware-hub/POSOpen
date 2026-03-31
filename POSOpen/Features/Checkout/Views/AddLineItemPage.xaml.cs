using POSOpen.Features.Checkout.ViewModels;

namespace POSOpen.Features.Checkout.Views;

public partial class AddLineItemPage : ContentPage
{
	public AddLineItemPage(AddLineItemViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
