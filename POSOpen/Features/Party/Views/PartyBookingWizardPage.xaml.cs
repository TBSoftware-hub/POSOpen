using POSOpen.Features.Party.ViewModels;

namespace POSOpen.Features.Party.Views;

public partial class PartyBookingWizardPage : ContentPage
{
	public PartyBookingWizardPage(PartyBookingWizardViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (BindingContext is PartyBookingWizardViewModel viewModel)
		{
			await viewModel.InitializeCommand.ExecuteAsync(null);
		}
	}
}
