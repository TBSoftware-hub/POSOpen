using POSOpen.Application.UseCases.Shell;
using POSOpen.Features.Shell.ViewModels;

namespace POSOpen.Features.Shell.Views;

public partial class ManagerOperationsPage : ContentPage
{
	private readonly ExecuteManagerOperationUseCase _executeManagerOperationUseCase;

	public ManagerOperationsPage(
		ManagerOperationsViewModel viewModel,
		ExecuteManagerOperationUseCase executeManagerOperationUseCase)
	{
		_executeManagerOperationUseCase = executeManagerOperationUseCase;
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		var result = _executeManagerOperationUseCase.Execute();
		if (result.IsSuccess)
		{
			return;
		}

		await global::Microsoft.Maui.Controls.Shell.Current.DisplayAlertAsync("Access denied", result.UserMessage, "OK");
		await global::Microsoft.Maui.Controls.Shell.Current.GoToAsync($"//{ShellRoutes.Home}");
	}
}
