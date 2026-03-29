using POSOpen.Features.Security.ViewModels;

namespace POSOpen.Features.Security.Views;

public partial class OverrideApprovalPage : ContentPage, IQueryAttributable
{
	private readonly OverrideApprovalViewModel _viewModel;

	public OverrideApprovalPage(OverrideApprovalViewModel viewModel)
	{
		_viewModel = viewModel;
		InitializeComponent();
		BindingContext = viewModel;
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		query.TryGetValue("actionKey", out var actionKeyValue);
		query.TryGetValue("targetReference", out var targetReferenceValue);

		var actionKey = actionKeyValue?.ToString() ?? string.Empty;
		var targetReference = targetReferenceValue?.ToString() ?? string.Empty;
		_viewModel.InitializeContext(actionKey, targetReference);
	}

	private async void OnCancelClicked(object sender, EventArgs e)
	{
		await global::Microsoft.Maui.Controls.Shell.Current.GoToAsync("..");
	}
}
