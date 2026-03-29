using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.StaffManagement;
using POSOpen.Domain.Enums;

namespace POSOpen.Features.StaffManagement.ViewModels;

public partial class CreateStaffAccountViewModel : ObservableObject
{
	private readonly CreateStaffAccountUseCase _createStaffAccountUseCase;
	private readonly IOperationContextFactory _operationContextFactory;

	[ObservableProperty]
	private ViewModelState _pageState = ViewModelState.Idle;

	[ObservableProperty]
	private string _firstName = string.Empty;

	[ObservableProperty]
	private string _lastName = string.Empty;

	[ObservableProperty]
	private string _email = string.Empty;

	[ObservableProperty]
	private string _password = string.Empty;

	[ObservableProperty]
	private StaffRole _selectedRole = StaffRole.Cashier;

	[ObservableProperty]
	private string _firstNameError = string.Empty;

	[ObservableProperty]
	private string _lastNameError = string.Empty;

	[ObservableProperty]
	private string _emailError = string.Empty;

	[ObservableProperty]
	private string _passwordError = string.Empty;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasSummaryError))]
	private string _summaryError = string.Empty;

	public CreateStaffAccountViewModel(
		CreateStaffAccountUseCase createStaffAccountUseCase,
		IOperationContextFactory operationContextFactory)
	{
		_createStaffAccountUseCase = createStaffAccountUseCase;
		_operationContextFactory = operationContextFactory;
	}

	public ObservableCollection<StaffRole> Roles { get; } = new(Enum.GetValues<StaffRole>());

	public bool IsBusy => PageState == ViewModelState.Loading;

	public bool HasSummaryError => !string.IsNullOrWhiteSpace(SummaryError);

	[RelayCommand]
	private void ValidateEmail()
	{
		if (string.IsNullOrWhiteSpace(Email))
		{
			EmailError = "Email is required.";
			return;
		}

		if (!Email.Contains('@', StringComparison.Ordinal))
		{
			EmailError = "Enter a valid email address.";
			return;
		}

		EmailError = string.Empty;
	}

	[RelayCommand]
	private async Task SubmitAsync()
	{
		if (PageState == ViewModelState.Loading)
		{
			return;
		}

		ClearErrors();
		if (!ValidateAll())
		{
			PageState = ViewModelState.Error;
			SummaryError = "Resolve the highlighted errors and try again.";
			return;
		}

		PageState = ViewModelState.Loading;
		OnPropertyChanged(nameof(IsBusy));

		var context = _operationContextFactory.CreateRoot();
		var command = new CreateStaffAccountCommand(
			FirstName,
			LastName,
			Email,
			Password,
			SelectedRole,
			context,
			CreatedByStaffId: null);

		var result = await _createStaffAccountUseCase.ExecuteAsync(command);
		if (!result.IsSuccess)
		{
			PageState = ViewModelState.Error;
			MapResultErrors(result.ErrorCode, result.UserMessage);
			OnPropertyChanged(nameof(IsBusy));
			return;
		}

		PageState = ViewModelState.Success;
		OnPropertyChanged(nameof(IsBusy));
		await global::Microsoft.Maui.Controls.Shell.Current.DisplayAlertAsync("Staff", "Staff account created.", "OK");
		await global::Microsoft.Maui.Controls.Shell.Current.GoToAsync("..");
	}

	private bool ValidateAll()
	{
		var isValid = true;
		if (string.IsNullOrWhiteSpace(FirstName))
		{
			FirstNameError = "First name is required.";
			isValid = false;
		}

		if (string.IsNullOrWhiteSpace(LastName))
		{
			LastNameError = "Last name is required.";
			isValid = false;
		}

		ValidateEmail();
		if (!string.IsNullOrWhiteSpace(EmailError))
		{
			isValid = false;
		}

		if (string.IsNullOrWhiteSpace(Password))
		{
			PasswordError = "Password is required.";
			isValid = false;
		}
		else if (Password.Length < CreateStaffAccountUseCase.MinPasswordLength)
		{
			PasswordError = $"Password must be at least {CreateStaffAccountUseCase.MinPasswordLength} characters.";
			isValid = false;
		}

		return isValid;
	}

	private void ClearErrors()
	{
		FirstNameError = string.Empty;
		LastNameError = string.Empty;
		EmailError = string.Empty;
		PasswordError = string.Empty;
		SummaryError = string.Empty;
	}

	private void MapResultErrors(string? errorCode, string userMessage)
	{
		if (errorCode == "STAFF_EMAIL_CONFLICT")
		{
			EmailError = userMessage;
			return;
		}

		SummaryError = userMessage;
	}
}
