using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.StaffManagement;
using POSOpen.Domain.Enums;

namespace POSOpen.Features.StaffManagement.ViewModels;

public partial class EditStaffAccountViewModel : ObservableObject
{
	private readonly GetStaffAccountByIdUseCase _getStaffAccountByIdUseCase;
	private readonly UpdateStaffAccountUseCase _updateStaffAccountUseCase;
	private readonly DeactivateStaffAccountUseCase _deactivateStaffAccountUseCase;
	private readonly IOperationContextFactory _operationContextFactory;

	[ObservableProperty]
	private ViewModelState _pageState = ViewModelState.Idle;

	[ObservableProperty]
	private Guid _staffAccountId;

	[ObservableProperty]
	private string _firstName = string.Empty;

	[ObservableProperty]
	private string _lastName = string.Empty;

	[ObservableProperty]
	private string _email = string.Empty;

	[ObservableProperty]
	private StaffRole _selectedRole = StaffRole.Cashier;

	[ObservableProperty]
	private string _firstNameError = string.Empty;

	[ObservableProperty]
	private string _lastNameError = string.Empty;

	[ObservableProperty]
	private string _emailError = string.Empty;

	[ObservableProperty]
	private string _summaryError = string.Empty;

	public EditStaffAccountViewModel(
		GetStaffAccountByIdUseCase getStaffAccountByIdUseCase,
		UpdateStaffAccountUseCase updateStaffAccountUseCase,
		DeactivateStaffAccountUseCase deactivateStaffAccountUseCase,
		IOperationContextFactory operationContextFactory)
	{
		_getStaffAccountByIdUseCase = getStaffAccountByIdUseCase;
		_updateStaffAccountUseCase = updateStaffAccountUseCase;
		_deactivateStaffAccountUseCase = deactivateStaffAccountUseCase;
		_operationContextFactory = operationContextFactory;
	}

	public ObservableCollection<StaffRole> Roles { get; } = new(Enum.GetValues<StaffRole>());

	public bool IsBusy => PageState == ViewModelState.Loading;

	public bool HasSummaryError => !string.IsNullOrWhiteSpace(SummaryError);

	public async Task LoadAsync(Guid staffAccountId)
	{
		PageState = ViewModelState.Loading;
		OnPropertyChanged(nameof(IsBusy));
		SummaryError = string.Empty;

		var result = await _getStaffAccountByIdUseCase.ExecuteAsync(staffAccountId);
		if (!result.IsSuccess || result.Payload is null)
		{
			PageState = ViewModelState.Error;
			SummaryError = result.UserMessage;
			OnPropertyChanged(nameof(HasSummaryError));
			OnPropertyChanged(nameof(IsBusy));
			return;
		}

		var account = result.Payload;
		StaffAccountId = account.Id;
		FirstName = account.FirstName;
		LastName = account.LastName;
		Email = account.Email;
		SelectedRole = account.Role;
		PageState = ViewModelState.Success;
		OnPropertyChanged(nameof(IsBusy));
	}

	[RelayCommand]
	private async Task SaveAsync()
	{
		ClearErrors();
		if (!ValidateAll())
		{
			PageState = ViewModelState.Error;
			SummaryError = "Resolve the highlighted errors and try again.";
			OnPropertyChanged(nameof(HasSummaryError));
			return;
		}

		PageState = ViewModelState.Loading;
		OnPropertyChanged(nameof(IsBusy));

		var context = _operationContextFactory.CreateRoot();
		var command = new UpdateStaffAccountCommand(
			StaffAccountId,
			FirstName,
			LastName,
			Email,
			SelectedRole,
			context,
			UpdatedByStaffId: Guid.Empty);

		var result = await _updateStaffAccountUseCase.ExecuteAsync(command);
		if (!result.IsSuccess)
		{
			PageState = ViewModelState.Error;
			if (result.ErrorCode == "STAFF_EMAIL_CONFLICT")
			{
				EmailError = result.UserMessage;
			}
			else
			{
				SummaryError = result.UserMessage;
			}

			OnPropertyChanged(nameof(HasSummaryError));
			OnPropertyChanged(nameof(IsBusy));
			return;
		}

		PageState = ViewModelState.Success;
		OnPropertyChanged(nameof(IsBusy));
		await global::Microsoft.Maui.Controls.Shell.Current.DisplayAlertAsync("Staff", "Staff account updated.", "OK");
		await global::Microsoft.Maui.Controls.Shell.Current.GoToAsync("..");
	}

	[RelayCommand]
	private async Task DeactivateAsync()
	{
		var confirmed = await global::Microsoft.Maui.Controls.Shell.Current.DisplayAlertAsync(
			"Deactivate account",
			$"Deactivate {FirstName} {LastName}'s account? They will no longer be able to sign in.",
			"Deactivate",
			"Cancel");
		if (!confirmed)
		{
			return;
		}

		PageState = ViewModelState.Loading;
		OnPropertyChanged(nameof(IsBusy));

		var context = _operationContextFactory.CreateRoot();
		var command = new DeactivateStaffAccountCommand(StaffAccountId, context, Guid.Empty);
		var result = await _deactivateStaffAccountUseCase.ExecuteAsync(command);
		if (!result.IsSuccess)
		{
			PageState = ViewModelState.Error;
			SummaryError = result.UserMessage;
			OnPropertyChanged(nameof(HasSummaryError));
			OnPropertyChanged(nameof(IsBusy));
			return;
		}

		PageState = ViewModelState.Success;
		OnPropertyChanged(nameof(IsBusy));
		await global::Microsoft.Maui.Controls.Shell.Current.DisplayAlertAsync("Staff", "Staff account deactivated.", "OK");
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

		if (string.IsNullOrWhiteSpace(Email))
		{
			EmailError = "Email is required.";
			isValid = false;
		}
		else if (!Email.Contains('@', StringComparison.Ordinal))
		{
			EmailError = "Enter a valid email address.";
			isValid = false;
		}

		return isValid;
	}

	private void ClearErrors()
	{
		FirstNameError = string.Empty;
		LastNameError = string.Empty;
		EmailError = string.Empty;
		SummaryError = string.Empty;
	}
}
