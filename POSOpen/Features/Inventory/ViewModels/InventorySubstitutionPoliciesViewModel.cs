using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Inventory;
using POSOpen.Domain.Enums;

namespace POSOpen.Features.Inventory.ViewModels;

public partial class InventorySubstitutionPoliciesViewModel : ObservableObject
{
	private readonly GetInventorySubstitutionPoliciesUseCase _listUseCase;
	private readonly CreateInventorySubstitutionPolicyUseCase _createUseCase;
	private readonly UpdateInventorySubstitutionPolicyUseCase _updateUseCase;
	private readonly DeleteInventorySubstitutionPolicyUseCase _deleteUseCase;
	private readonly IOperationContextFactory _operationContextFactory;

	[ObservableProperty]
	private bool _isBusy;

	[ObservableProperty]
	private string _sourceOptionId = string.Empty;

	[ObservableProperty]
	private string _allowedSubstituteOptionId = string.Empty;

	[ObservableProperty]
	private bool _isActive = true;

	[ObservableProperty]
	private bool _allowOwnerRole = true;

	[ObservableProperty]
	private bool _allowAdminRole = true;

	[ObservableProperty]
	private bool _allowManagerRole = true;

	[ObservableProperty]
	private bool _allowCashierRole;

	[ObservableProperty]
	private string _sourceError = string.Empty;

	[ObservableProperty]
	private string _substituteError = string.Empty;

	[ObservableProperty]
	private string _rolesError = string.Empty;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasSummaryError))]
	private string _summaryError = string.Empty;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasStatusMessage))]
	private string _statusMessage = string.Empty;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanDelete))]
	private Guid? _selectedPolicyId;

	public InventorySubstitutionPoliciesViewModel(
		GetInventorySubstitutionPoliciesUseCase listUseCase,
		CreateInventorySubstitutionPolicyUseCase createUseCase,
		UpdateInventorySubstitutionPolicyUseCase updateUseCase,
		DeleteInventorySubstitutionPolicyUseCase deleteUseCase,
		IOperationContextFactory operationContextFactory)
	{
		_listUseCase = listUseCase;
		_createUseCase = createUseCase;
		_updateUseCase = updateUseCase;
		_deleteUseCase = deleteUseCase;
		_operationContextFactory = operationContextFactory;
	}

	public ObservableCollection<InventorySubstitutionPolicyManagementDto> Policies { get; } = new();

	public bool HasSummaryError => !string.IsNullOrWhiteSpace(SummaryError);

	public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

	public bool CanDelete => SelectedPolicyId.HasValue;

	[RelayCommand]
	private async Task LoadAsync()
	{
		if (IsBusy)
		{
			return;
		}

		IsBusy = true;
		SummaryError = string.Empty;
		StatusMessage = string.Empty;

		await RefreshPoliciesAsync();

		IsBusy = false;
	}

	[RelayCommand]
	private async Task SaveAsync()
	{
		if (IsBusy)
		{
			return;
		}

		ClearErrors();
		var roles = GetSelectedRoles();
		if (!ValidateLocal(roles))
		{
			SummaryError = "Resolve the highlighted errors and try again.";
			return;
		}

		IsBusy = true;
		var context = _operationContextFactory.CreateRoot();

		if (SelectedPolicyId.HasValue)
		{
			var result = await _updateUseCase.ExecuteAsync(
				new UpdateInventorySubstitutionPolicyCommand(
					SelectedPolicyId.Value,
					SourceOptionId,
					AllowedSubstituteOptionId,
					roles,
					IsActive,
					context));
			if (!result.IsSuccess)
			{
				MapResultErrors(result.ErrorCode, result.UserMessage);
				IsBusy = false;
				return;
			}

			StatusMessage = result.UserMessage;
		}
		else
		{
			var result = await _createUseCase.ExecuteAsync(
				new CreateInventorySubstitutionPolicyCommand(
					SourceOptionId,
					AllowedSubstituteOptionId,
					roles,
					IsActive,
					context));
			if (!result.IsSuccess)
			{
				MapResultErrors(result.ErrorCode, result.UserMessage);
				IsBusy = false;
				return;
			}

			StatusMessage = result.UserMessage;
			ResetForm();
		}

		await RefreshPoliciesAsync();
		IsBusy = false;
	}

	[RelayCommand]
	private async Task DeleteAsync()
	{
		if (IsBusy || !SelectedPolicyId.HasValue)
		{
			return;
		}

		IsBusy = true;
		SummaryError = string.Empty;
		StatusMessage = string.Empty;

		var result = await _deleteUseCase.ExecuteAsync(new DeleteInventorySubstitutionPolicyCommand(
			SelectedPolicyId.Value,
			_operationContextFactory.CreateRoot()));
		if (!result.IsSuccess)
		{
			MapResultErrors(result.ErrorCode, result.UserMessage);
			IsBusy = false;
			return;
		}

		StatusMessage = result.UserMessage;
		ResetForm();
		await RefreshPoliciesAsync();
		IsBusy = false;
	}

	[RelayCommand]
	private void StartCreate()
	{
		ResetForm();
		ClearErrors();
		StatusMessage = string.Empty;
	}

	[RelayCommand]
	private void EditPolicy(InventorySubstitutionPolicyManagementDto? policy)
	{
		if (policy is null)
		{
			return;
		}

		SelectedPolicyId = policy.PolicyId;
		SourceOptionId = policy.SourceOptionId;
		AllowedSubstituteOptionId = policy.AllowedSubstituteOptionId;
		IsActive = policy.IsActive;
		AllowOwnerRole = policy.AllowedRoles.Contains(StaffRole.Owner);
		AllowAdminRole = policy.AllowedRoles.Contains(StaffRole.Admin);
		AllowManagerRole = policy.AllowedRoles.Contains(StaffRole.Manager);
		AllowCashierRole = policy.AllowedRoles.Contains(StaffRole.Cashier);
		ClearErrors();
		StatusMessage = string.Empty;
	}

	private bool ValidateLocal(IReadOnlyList<StaffRole> roles)
	{
		var valid = true;

		if (string.IsNullOrWhiteSpace(SourceOptionId))
		{
			SourceError = "Source item is required.";
			valid = false;
		}

		if (string.IsNullOrWhiteSpace(AllowedSubstituteOptionId))
		{
			SubstituteError = "Substitute item is required.";
			valid = false;
		}

		if (roles.Count == 0)
		{
			RolesError = "Choose at least one role.";
			valid = false;
		}

		return valid;
	}

	private IReadOnlyList<StaffRole> GetSelectedRoles()
	{
		var output = new List<StaffRole>();
		if (AllowOwnerRole)
		{
			output.Add(StaffRole.Owner);
		}

		if (AllowAdminRole)
		{
			output.Add(StaffRole.Admin);
		}

		if (AllowManagerRole)
		{
			output.Add(StaffRole.Manager);
		}

		if (AllowCashierRole)
		{
			output.Add(StaffRole.Cashier);
		}

		return output;
	}

	private void ClearErrors()
	{
		SourceError = string.Empty;
		SubstituteError = string.Empty;
		RolesError = string.Empty;
		SummaryError = string.Empty;
	}

	private async Task RefreshPoliciesAsync()
	{
		var result = await _listUseCase.ExecuteAsync(new GetInventorySubstitutionPoliciesQuery(_operationContextFactory.CreateRoot()));
		if (!result.IsSuccess || result.Payload is null)
		{
			SummaryError = result.UserMessage;
			return;
		}

		Policies.Clear();
		foreach (var policy in result.Payload)
		{
			Policies.Add(policy);
		}
	}

	private void MapResultErrors(string? errorCode, string userMessage)
	{
		switch (errorCode)
		{
			case InventorySubstitutionPolicyConstants.ErrorSourceRequired:
			case InventorySubstitutionPolicyConstants.ErrorSourceInvalid:
				SourceError = userMessage;
				break;
			case InventorySubstitutionPolicyConstants.ErrorSubstituteRequired:
			case InventorySubstitutionPolicyConstants.ErrorSubstituteInvalid:
				SubstituteError = userMessage;
				break;
			case InventorySubstitutionPolicyConstants.ErrorRoleRequired:
				RolesError = userMessage;
				break;
			default:
				SummaryError = userMessage;
				break;
		}
	}

	private void ResetForm()
	{
		SelectedPolicyId = null;
		SourceOptionId = string.Empty;
		AllowedSubstituteOptionId = string.Empty;
		IsActive = true;
		AllowOwnerRole = true;
		AllowAdminRole = true;
		AllowManagerRole = true;
		AllowCashierRole = false;
	}
}
