using POSOpen.Features.Admissions.ViewModels;

namespace POSOpen.Features.Admissions.Views;

[QueryProperty(nameof(FamilyId), "familyId")]
public partial class FastPathCheckInPage : ContentPage
{
	private readonly FastPathCheckInViewModel _viewModel;
	private bool _hasLoaded;

	public FastPathCheckInPage(FastPathCheckInViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
	}

	public string FamilyId
	{
		set
		{
			if (Guid.TryParse(value, out var parsedId))
			{
				_viewModel.Initialize(parsedId);
				_hasLoaded = false;
			}
		}
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (_hasLoaded)
		{
			await _viewModel.RefreshWaiverStatusCommand.ExecuteAsync(null);
			return;
		}

		await _viewModel.LoadAsync();
		_hasLoaded = true;
	}
}
