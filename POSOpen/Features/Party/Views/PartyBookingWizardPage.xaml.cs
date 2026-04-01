using POSOpen.Features.Party.ViewModels;
using POSOpen.Features.Party;

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

	private async void OpenBookingDetailClicked(object? sender, EventArgs e)
	{
		if (BindingContext is not PartyBookingWizardViewModel viewModel || viewModel.BookingId is null)
		{
			return;
		}

		await global::Microsoft.Maui.Controls.Shell.Current.GoToAsync(
			$"{PartyRoutes.PartyBookingDetail}?bookingId={viewModel.BookingId.Value}");
	}
}
