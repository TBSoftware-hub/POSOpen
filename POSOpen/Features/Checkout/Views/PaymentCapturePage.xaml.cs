using POSOpen.Features.Checkout.ViewModels;

namespace POSOpen.Features.Checkout.Views;

[QueryProperty(nameof(CartId), "cartId")]
public partial class PaymentCapturePage : ContentPage
{
	public PaymentCapturePage(PaymentCaptureViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	public string CartId
	{
		set
		{
			if (BindingContext is PaymentCaptureViewModel viewModel)
			{
				viewModel.CartId = value;
			}
		}
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (BindingContext is PaymentCaptureViewModel vm)
		{
			await vm.InitializeCommand.ExecuteAsync(null);
		}
	}
}