using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.StaffManagement;
using POSOpen.Domain.Enums;

namespace POSOpen.Features.StaffManagement.ViewModels;

public partial class EditStaffAccountViewModel : ObservableObject
{
	private readonly GetStaffAccountByIdUseCase _getStaffAccountByIdUseCase;
	private readonly UpdateStaffAccountUseCase _updateStaffAccountUseCase;
	private readonly AssignStaffRoleUseCase _assignStaffRoleUseCase;
	private readonly DeactivateStaffAccountUseCase _deactivateStaffAccountUseCase;
	private readonly ICurrentSessionService _currentSessionService;
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

	private StaffRole _originalRole = StaffRole.Cashier;
	private string _originalFirstName = string.Empty;
	private string _originalLastName = string.Empty;
	private string _originalEmail = string.Empty;

	[ObservableProperty]
	private string _firstNameError = string.Empty;

	[ObservableProperty]
	private string _lastNameError = string.Empty;

	[ObservableProperty]
	private string _emailError = string.Empty;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasSummaryError))]
	private string _summaryError = string.Empty;

	public EditStaffAccountViewModel(
		GetStaffAccountByIdUseCase getStaffAccountByIdUseCase,
		UpdateStaffAccountUseCase updateStaffAccountUseCase,
		AssignStaffRoleUseCase assignStaffRoleUseCase,
		DeactivateStaffAccountUseCase deactivateStaffAccountUseCase,
		ICurrentSessionService currentSessionService,
		IOperationContextFactory operationContextFactory)
	{
		_getStaffAccountByIdUseCase = getStaffAccountByIdUseCase;
		_updateStaffAccountUseCase = updateStaffAccountUseCase;
		_assignStaffRoleUseCase = assignStaffRoleUseCase;
		_deactivateStaffAccountUseCase = deactivateStaffAccountUseCase;
		_currentSessionService = currentSessionService;
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
			OnPropertyChanged(nameof(IsBusy));
			return;
		}

		var account = result.Payload;
		StaffAccountId = account.Id;
		FirstName = account.FirstName;
		LastName = account.LastName;
		Email = account.Email;
		_originalFirstName = account.FirstName;
		_originalLastName = account.LastName;
		_originalEmail = account.Email;
		SelectedRole = account.Role;
		_originalRole = account.Role;
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
			return;
		}

		PageState = ViewModelState.Loading;
		OnPropertyChanged(nameof(IsBusy));

		var session = _currentSessionService.GetCurrent();
		if (session is null)
		{
			PageState = ViewModelState.Error;
			SummaryError = "You do not have access to this action.";
			OnPropertyChanged(nameof(IsBusy));
			return;
		}

		var context = _operationContextFactory.CreateRoot();
		var command = new UpdateStaffAccountCommand(
			StaffAccountId,
			FirstName,
			LastName,
			Email,
			SelectedRole,
			context,
			UpdatedByStaffId: session.StaffId);

		var result = await _updateStaffAccountUseCase.ExecuteAsync(command);
		if (!result.IsSuccess)
		{
			PageState = ViewModelState.Error;
			if (result.ErrorCode == "STAFF_EMAIL_CONFLICT")
			{
				EmailError = result.UserMessage;
			}
			else if (result.ErrorCode == "AUTH_FORBIDDEN")
			{
				SummaryError = "You do not have access to this action.";
			}
			else
			{
				SummaryError = result.UserMessage;
			}

			OnPropertyChanged(nameof(IsBusy));
			return;
		}

		if (_originalRole != SelectedRole)
		{
			var roleContext = _operationContextFactory.CreateChild(context.CorrelationId, context.OperationId);
			var roleResult = await _assignStaffRoleUseCase.ExecuteAsync(
				new AssignStaffRoleCommand(StaffAccountId, SelectedRole, roleContext));

			if (!roleResult.IsSuccess)
			{
				await RestoreOriginalProfileAsync(session.StaffId, context);
				PageState = ViewModelState.Error;
				SummaryError = roleResult.ErrorCode == "AUTH_FORBIDDEN"
					? "You do not have access to this action."
					: roleResult.UserMessage;
				OnPropertyChanged(nameof(IsBusy));
				return;
			}

			_originalRole = SelectedRole;
		}

		_originalFirstName = FirstName;
		_originalLastName = LastName;
		_originalEmail = Email;

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

	private Task RestoreOriginalProfileAsync(Guid actorStaffId, Shared.Operational.OperationContext parentContext)
	{
		var rollbackContext = _operationContextFactory.CreateChild(parentContext.CorrelationId, parentContext.OperationId);
		var rollbackCommand = new UpdateStaffAccountCommand(
			StaffAccountId,
			_originalFirstName,
			_originalLastName,
			_originalEmail,
			_originalRole,
			rollbackContext,
			actorStaffId);

		return _updateStaffAccountUseCase.ExecuteAsync(rollbackCommand);
	}
}
