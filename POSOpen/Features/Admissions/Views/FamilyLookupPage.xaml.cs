using POSOpen.Features.Admissions.ViewModels;

namespace POSOpen.Features.Admissions.Views;

public partial class FamilyLookupPage : ContentPage
{
	public FamilyLookupPage(FamilyLookupViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
