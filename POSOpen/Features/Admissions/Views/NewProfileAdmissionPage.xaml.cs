using POSOpen.Features.Admissions.ViewModels;

namespace POSOpen.Features.Admissions.Views;

[QueryProperty(nameof(FamilyId), "familyId")]
[QueryProperty(nameof(Hint), "hint")]
public partial class NewProfileAdmissionPage : ContentPage
{
	private readonly NewProfileAdmissionViewModel _viewModel;
	private Guid? _familyId;
	private string? _hint;

	public NewProfileAdmissionPage(NewProfileAdmissionViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
	}

	public string? FamilyId
	{
		set
		{
			_familyId = Guid.TryParse(value, out var parsed) ? parsed : null;
			_viewModel.SetRouteInput(_familyId, _hint);
		}
	}

	public string? Hint
	{
		set
		{
			_hint = value;
			_viewModel.SetRouteInput(_familyId, _hint);
		}
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.LoadAsync();
	}
}
