using CommunityToolkit.Mvvm.ComponentModel;

namespace POSOpen.Features.Shell.ViewModels;

public partial class ManagerOperationsViewModel : ObservableObject
{
	[ObservableProperty]
	private string _title = "Manager Operations";

	[ObservableProperty]
	private string _summary = "Manager-only operational tools are available in this workspace.";
}
