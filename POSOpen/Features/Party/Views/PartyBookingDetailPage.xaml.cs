using POSOpen.Features.Party.ViewModels;

namespace POSOpen.Features.Party.Views;

public partial class PartyBookingDetailPage : ContentPage, IQueryAttributable
{
	private Guid? _bookingId;

	public PartyBookingDetailPage(PartyBookingDetailViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (!query.TryGetValue("bookingId", out var value) || value is null)
		{
			_bookingId = null;
			return;
		}

		var candidate = Uri.UnescapeDataString(value.ToString() ?? string.Empty);
		if (Guid.TryParse(candidate, out var parsed))
		{
			_bookingId = parsed;
			return;
		}

		_bookingId = null;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (_bookingId.HasValue && BindingContext is PartyBookingDetailViewModel viewModel)
		{
			await viewModel.LoadAsync(_bookingId.Value);
		}
	}
}
