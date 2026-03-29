using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Authentication;
using POSOpen.Features.StaffManagement.ViewModels;

namespace POSOpen.Features.Authentication.ViewModels;

public partial class SignInViewModel : ObservableObject
{
	private readonly AuthenticateStaffUseCase _authenticateStaffUseCase;
	private readonly IOperationContextFactory _operationContextFactory;
	private readonly IAuthenticationPerformanceTracker _authenticationPerformanceTracker;
	private readonly IUtcClock _clock;

	[ObservableProperty]
	private ViewModelState _pageState = ViewModelState.Idle;

	[ObservableProperty]
	private string _email = string.Empty;

	[ObservableProperty]
	private string _password = string.Empty;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasError))]
	private string _errorMessage = string.Empty;

	public SignInViewModel(
		AuthenticateStaffUseCase authenticateStaffUseCase,
		IOperationContextFactory operationContextFactory,
		IAuthenticationPerformanceTracker authenticationPerformanceTracker,
		IUtcClock clock)
	{
		_authenticateStaffUseCase = authenticateStaffUseCase;
		_operationContextFactory = operationContextFactory;
		_authenticationPerformanceTracker = authenticationPerformanceTracker;
		_clock = clock;
	}

	public bool IsBusy => PageState == ViewModelState.Loading;

	public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

	[RelayCommand]
	private async Task SignInAsync()
	{
		if (PageState == ViewModelState.Loading)
		{
			return;
		}

		PageState = ViewModelState.Loading;
		ErrorMessage = string.Empty;
		OnPropertyChanged(nameof(IsBusy));

		_authenticationPerformanceTracker.MarkSignInStarted(_clock.UtcNow);
		var command = new AuthenticateStaffCommand(Email, Password, _operationContextFactory.CreateRoot());
		var result = await _authenticateStaffUseCase.ExecuteAsync(command);

		if (!result.IsSuccess || result.Payload is null)
		{
			Password = string.Empty;
			PageState = ViewModelState.Error;
			ErrorMessage = result.UserMessage;
			OnPropertyChanged(nameof(IsBusy));
			return;
		}

		Password = string.Empty;

		try
		{
			PageState = ViewModelState.Success;
			ErrorMessage = string.Empty;
			await global::Microsoft.Maui.Controls.Shell.Current.GoToAsync($"//{result.Payload.NextRoute}");
		}
		catch
		{
			PageState = ViewModelState.Error;
			ErrorMessage = AuthenticationConstants.SafeRoleHomeUnavailableMessage;
		}
		finally
		{
			OnPropertyChanged(nameof(IsBusy));
		}
	}
}
