using POSOpen.Features.Checkout.ViewModels;

namespace POSOpen.Features.Checkout.Views;

[QueryProperty(nameof(CartSessionIdParam), "cartSessionId")]
public partial class RefundWorkflowPage : ContentPage
{
	public RefundWorkflowPage(RefundWorkflowViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	public string CartSessionIdParam
	{
		set
		{
			if (BindingContext is RefundWorkflowViewModel viewModel)
			{
				viewModel.CartSessionIdParam = value;
			}
		}
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (BindingContext is RefundWorkflowViewModel vm)
		{
			await vm.InitializeCommand.ExecuteAsync(null);
		}
	}
}