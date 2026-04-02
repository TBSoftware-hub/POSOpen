using POSOpen.Application.UseCases.Shell;
using POSOpen.Features.Inventory.ViewModels;

namespace POSOpen.Features.Inventory.Views;

public partial class InventorySubstitutionPoliciesPage : ContentPage
{
	private readonly InventorySubstitutionPoliciesViewModel _viewModel;
	private readonly ExecuteManagerOperationUseCase _executeManagerOperationUseCase;

	public InventorySubstitutionPoliciesPage(
		InventorySubstitutionPoliciesViewModel viewModel,
		ExecuteManagerOperationUseCase executeManagerOperationUseCase)
	{
		InitializeComponent();
		_viewModel = viewModel;
		_executeManagerOperationUseCase = executeManagerOperationUseCase;
		BindingContext = _viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		var accessResult = _executeManagerOperationUseCase.Execute();
		if (!accessResult.IsSuccess)
		{
			await global::Microsoft.Maui.Controls.Shell.Current.DisplayAlertAsync("Access denied", accessResult.UserMessage, "OK");
			await global::Microsoft.Maui.Controls.Shell.Current.GoToAsync("..");
			return;
		}

		await _viewModel.LoadCommand.ExecuteAsync(null);
	}
}
