using POSOpen.Features.StaffManagement.ViewModels;

namespace POSOpen.Features.StaffManagement.Views;

[QueryProperty(nameof(StaffId), "staffId")]
public partial class EditStaffAccountPage : ContentPage
{
	private readonly EditStaffAccountViewModel _viewModel;
	private Guid? _staffId;

	public EditStaffAccountPage(EditStaffAccountViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
	}

	public string StaffId
	{
		set
		{
			if (Guid.TryParse(value, out var parsedId))
			{
				_staffId = parsedId;
			}
		}
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (_staffId.HasValue)
		{
			await _viewModel.LoadAsync(_staffId.Value);
		}
	}
}
