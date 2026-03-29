using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POSOpen.Application.UseCases.StaffManagement;

namespace POSOpen.Features.StaffManagement.ViewModels;

public partial class StaffListViewModel : ObservableObject
{
	private readonly ListActiveStaffAccountsUseCase _listActiveStaffAccountsUseCase;

	[ObservableProperty]
	private ViewModelState _pageState = ViewModelState.Idle;

	[ObservableProperty]
	private string _errorMessage = string.Empty;

	public StaffListViewModel(ListActiveStaffAccountsUseCase listActiveStaffAccountsUseCase)
	{
		_listActiveStaffAccountsUseCase = listActiveStaffAccountsUseCase;
	}

	public ObservableCollection<StaffAccountDto> StaffAccounts { get; } = new();

	public bool IsBusy => PageState == ViewModelState.Loading;

	public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

	public bool IsEmpty => !IsBusy && !HasError && StaffAccounts.Count == 0;

	[RelayCommand]
	private async Task LoadAsync()
	{
		if (PageState == ViewModelState.Loading)
		{
			return;
		}

		PageState = ViewModelState.Loading;
		OnPropertyChanged(nameof(IsBusy));
		OnPropertyChanged(nameof(IsEmpty));
		ErrorMessage = string.Empty;
		OnPropertyChanged(nameof(HasError));

		var result = await _listActiveStaffAccountsUseCase.ExecuteAsync();
		if (!result.IsSuccess || result.Payload is null)
		{
			PageState = ViewModelState.Error;
			ErrorMessage = result.UserMessage;
			OnPropertyChanged(nameof(HasError));
			OnPropertyChanged(nameof(IsBusy));
			OnPropertyChanged(nameof(IsEmpty));
			return;
		}

		StaffAccounts.Clear();
		foreach (var account in result.Payload)
		{
			StaffAccounts.Add(account);
		}

		PageState = ViewModelState.Success;
		OnPropertyChanged(nameof(IsBusy));
		OnPropertyChanged(nameof(IsEmpty));
	}

	[RelayCommand]
	private async Task NavigateToCreateAsync()
	{
		await global::Microsoft.Maui.Controls.Shell.Current.GoToAsync(StaffManagementRoutes.CreateStaff);
	}

	[RelayCommand]
	private async Task NavigateToEditAsync(StaffAccountDto? account)
	{
		if (account is null)
		{
			return;
		}

		await global::Microsoft.Maui.Controls.Shell.Current.GoToAsync($"{StaffManagementRoutes.EditStaff}?staffId={account.Id}");
	}
}
