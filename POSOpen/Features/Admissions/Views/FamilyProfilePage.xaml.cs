namespace POSOpen.Features.Admissions.Views;

[QueryProperty(nameof(FamilyId), "familyId")]
public partial class FamilyProfilePage : ContentPage
{
	private Guid? _familyId;

	public FamilyProfilePage()
	{
		InitializeComponent();
	}

	public string? FamilyId
	{
		set
		{
			if (Guid.TryParse(value, out var parsedId))
			{
				_familyId = parsedId;
				FamilyIdLabel.Text = $"Selected family: {_familyId}";
				return;
			}

			FamilyIdLabel.Text = "Selected family: unavailable";
		}
	}
}
