using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POSOpen.Features.Inventory;

namespace POSOpen.Features.Shell.ViewModels;

public partial class ManagerOperationsViewModel : ObservableObject
{
	[ObservableProperty]
	private string _title = "Manager Operations";

	[ObservableProperty]
	private string _summary = "Manager-only operational tools are available in this workspace.";

	[RelayCommand]
	private async Task OpenSubstitutionPoliciesAsync()
	{
		await global::Microsoft.Maui.Controls.Shell.Current.GoToAsync(InventoryRoutes.SubstitutionPolicies);
	}
}
