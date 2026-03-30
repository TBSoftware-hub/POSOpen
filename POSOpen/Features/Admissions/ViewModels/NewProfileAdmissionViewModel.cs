using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Admissions;

namespace POSOpen.Features.Admissions.ViewModels;

public partial class NewProfileAdmissionViewModel : ObservableObject
{
	private readonly ProfileAdmissionUseCase _profileAdmissionUseCase;
	private readonly IProfileAdmissionUiService _uiService;

	private Guid? _familyId;
	private string? _hint;
	private bool _hasLoaded;

	public NewProfileAdmissionViewModel(
		ProfileAdmissionUseCase profileAdmissionUseCase,
		IProfileAdmissionUiService uiService)
	{
		_profileAdmissionUseCase = profileAdmissionUseCase;
		_uiService = uiService;
	}

	[ObservableProperty]
	private bool _isLoading;

	[ObservableProperty]
	private bool _isExistingProfile;

	[ObservableProperty]
	private string _firstName = string.Empty;

	[ObservableProperty]
	private string _lastName = string.Empty;

	[ObservableProperty]
	private string _phone = string.Empty;

	[ObservableProperty]
	private string _email = string.Empty;

	[ObservableProperty]
	private string _firstNameError = string.Empty;

	[ObservableProperty]
	private string _lastNameError = string.Empty;

	[ObservableProperty]
	private string _phoneError = string.Empty;

	[ObservableProperty]
	private string _emailError = string.Empty;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasSummaryError))]
	private string _summaryError = string.Empty;

	[ObservableProperty]
	private string _nextBestAction = "Complete required profile fields to continue admission.";

	[ObservableProperty]
	private bool _showFirstNameField = true;

	[ObservableProperty]
	private bool _showLastNameField = true;

	[ObservableProperty]
	private bool _showPhoneField = true;

	public bool HasSummaryError => !string.IsNullOrWhiteSpace(SummaryError);

	public void SetRouteInput(Guid? familyId, string? hint)
	{
		_familyId = familyId;
		_hint = hint;
		_hasLoaded = false;
	}

	public async Task LoadAsync(CancellationToken ct = default)
	{
		if (_hasLoaded)
		{
			return;
		}

		IsLoading = true;
		ClearErrors();

		var result = await _profileAdmissionUseCase.InitializeAsync(
			new InitializeProfileAdmissionDraftQuery(_familyId, _hint),
			ct);

		IsLoading = false;
		if (!result.IsSuccess || result.Payload is null)
		{
			SummaryError = result.UserMessage;
			return;
		}

		var draft = result.Payload;
		_familyId = draft.FamilyId;
		IsExistingProfile = draft.IsExistingProfile;
		FirstName = draft.FirstName;
		LastName = draft.LastName;
		Phone = draft.Phone;
		Email = draft.Email ?? string.Empty;

		if (draft.MissingRequiredFields.Count > 0)
		{
			NextBestAction = "Fill only missing required fields to continue admission.";
		}
		else
		{
			NextBestAction = "Profile is complete. Continue admission.";
		}

		ApplyFieldVisibility(draft.IsExistingProfile, draft.MissingRequiredFields);

		_hasLoaded = true;
	}

	[RelayCommand]
	private async Task SubmitAsync(CancellationToken ct = default)
	{
		ClearErrors();
		if (!ValidateInput())
		{
			SummaryError = ProfileAdmissionConstants.SafeRequiredFieldsMissingMessage;
			return;
		}

		IsLoading = true;
		var result = await _profileAdmissionUseCase.SubmitAsync(
			new SubmitProfileAdmissionCommand(
				_familyId,
				FirstName,
				LastName,
				Phone,
				string.IsNullOrWhiteSpace(Email) ? null : Email),
			ct);
		IsLoading = false;

		if (!result.IsSuccess || result.Payload is null)
		{
			SummaryError = result.UserMessage;
			return;
		}

		try
		{
			await _uiService.NavigateToFastPathCheckInAsync(result.Payload.FamilyId);
		}
		catch
		{
			SummaryError = ProfileAdmissionConstants.SafeAdmissionRouteUnavailableMessage;
		}
	}

	private bool ValidateInput()
	{
		var isValid = true;

		if (ShowFirstNameField && string.IsNullOrWhiteSpace(FirstName))
		{
			FirstNameError = "First name is required.";
			isValid = false;
		}

		if (ShowLastNameField && string.IsNullOrWhiteSpace(LastName))
		{
			LastNameError = "Last name is required.";
			isValid = false;
		}

		if (ShowPhoneField && string.IsNullOrWhiteSpace(Phone))
		{
			PhoneError = "Phone is required.";
			isValid = false;
		}

		if (!string.IsNullOrWhiteSpace(Email) && !Email.Contains('@', StringComparison.Ordinal))
		{
			EmailError = "Enter a valid email address or leave it blank.";
			isValid = false;
		}

		return isValid;
	}

	private void ClearErrors()
	{
		FirstNameError = string.Empty;
		LastNameError = string.Empty;
		PhoneError = string.Empty;
		EmailError = string.Empty;
		SummaryError = string.Empty;
	}

	private void ApplyFieldVisibility(bool isExistingProfile, IReadOnlyList<string> missingRequiredFields)
	{
		if (!isExistingProfile || missingRequiredFields.Count == 0)
		{
			ShowFirstNameField = true;
			ShowLastNameField = true;
			ShowPhoneField = true;
			return;
		}

		ShowFirstNameField = missingRequiredFields.Contains("firstName", StringComparer.Ordinal);
		ShowLastNameField = missingRequiredFields.Contains("lastName", StringComparer.Ordinal);
		ShowPhoneField = missingRequiredFields.Contains("phone", StringComparer.Ordinal);
	}
}
