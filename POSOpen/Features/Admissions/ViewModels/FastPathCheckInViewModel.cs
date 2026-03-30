using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Admissions;
using POSOpen.Domain.Enums;

namespace POSOpen.Features.Admissions.ViewModels;

public partial class FastPathCheckInViewModel : ObservableObject
{
	private readonly EvaluateFastPathCheckInUseCase _evaluateFastPathCheckInUseCase;
	private readonly ProfileAdmissionUseCase _profileAdmissionUseCase;
	private readonly IFastPathCheckInUiService _uiService;

	public FastPathCheckInViewModel(
		EvaluateFastPathCheckInUseCase evaluateFastPathCheckInUseCase,
		ProfileAdmissionUseCase profileAdmissionUseCase,
		IFastPathCheckInUiService uiService)
	{
		_evaluateFastPathCheckInUseCase = evaluateFastPathCheckInUseCase;
		_profileAdmissionUseCase = profileAdmissionUseCase;
		_uiService = uiService;
	}

	public Guid? FamilyId { get; private set; }

	[ObservableProperty]
	private string _familyDisplayName = "Family";

	[ObservableProperty]
	private WaiverStatus _waiverStatus = WaiverStatus.None;

	[ObservableProperty]
	private string _waiverStatusLabel = "No Waiver";

	[ObservableProperty]
	private string _guidanceMessage = "Waiver status will be evaluated at check-in.";

	[ObservableProperty]
	private bool _isEligible;

	[ObservableProperty]
	private bool _showRecoveryAction;

	[ObservableProperty]
	private bool _showRefreshAction;

	[ObservableProperty]
	private bool _showProfileCompletionAction;

	[ObservableProperty]
	private bool _isLoading;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasError))]
	private string? _errorMessage;

	public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

	public void Initialize(Guid familyId)
	{
		FamilyId = familyId;
	}

	public async Task LoadAsync(CancellationToken ct = default)
	{
		await EvaluateAsync(isRefreshRequested: false, ct);
	}

	[RelayCommand]
	private async Task RefreshWaiverStatusAsync(CancellationToken ct = default)
	{
		await EvaluateAsync(isRefreshRequested: true, ct);
	}

	[RelayCommand]
	private async Task CompleteCheckInAsync()
	{
		await EvaluateAsync(isRefreshRequested: true, CancellationToken.None);
		if (HasError)
		{
			return;
		}

		if (!IsEligible)
		{
			ErrorMessage = "Fast-path completion is blocked until waiver requirements are satisfied.";
			return;
		}

		try
		{
			ErrorMessage = null;
			await _uiService.ShowFastPathReadyAsync();
		}
		catch
		{
			ErrorMessage = EvaluateFastPathCheckInConstants.SafeFastPathUnavailableMessage;
		}
	}

	[RelayCommand]
	private async Task StartWaiverRecoveryAsync()
	{
		if (FamilyId is null)
		{
			ErrorMessage = "Select a family before starting waiver recovery.";
			return;
		}

		ErrorMessage = null;
		try
		{
			await _uiService.NavigateToWaiverRecoveryAsync(FamilyId.Value);
		}
		catch
		{
			ErrorMessage = "Waiver recovery is not available on this terminal yet.";
		}
	}

	[RelayCommand]
	private async Task StartProfileCompletionAsync()
	{
		if (FamilyId is null)
		{
			ErrorMessage = "Select a family before completing profile details.";
			return;
		}

		ErrorMessage = null;
		try
		{
			await _uiService.NavigateToProfileCompletionAsync(FamilyId.Value);
		}
		catch
		{
			ErrorMessage = ProfileAdmissionConstants.SafeAdmissionRouteUnavailableMessage;
		}
	}

	private async Task EvaluateAsync(bool isRefreshRequested, CancellationToken ct)
	{
		if (FamilyId is null)
		{
			ErrorMessage = "Select a family before starting fast-path check-in.";
			return;
		}

		IsLoading = true;
		try
		{
			var result = await _evaluateFastPathCheckInUseCase.ExecuteAsync(
				new EvaluateFastPathCheckInQuery(FamilyId.Value, isRefreshRequested),
				ct);

			if (!result.IsSuccess || result.Payload is null)
			{
				ErrorMessage = result.UserMessage;
				return;
			}

			var payload = result.Payload;
			FamilyDisplayName = payload.FamilyDisplayName;
			WaiverStatus = payload.WaiverStatus;
			WaiverStatusLabel = payload.WaiverStatusLabel;
			GuidanceMessage = payload.GuidanceMessage;
			IsEligible = payload.IsEligible;
			ShowRecoveryAction = payload.ShowRecoveryAction;
			ShowRefreshAction = payload.ShowRefreshAction;
			ShowProfileCompletionAction = false;

			if (IsEligible && FamilyId is not null)
			{
				var draftResult = await _profileAdmissionUseCase.InitializeAsync(
					new InitializeProfileAdmissionDraftQuery(FamilyId.Value, null),
					ct);

				if (!draftResult.IsSuccess)
				{
					IsEligible = false;
					ShowProfileCompletionAction = false;
					ErrorMessage = draftResult.UserMessage;
					GuidanceMessage = "Profile completeness could not be verified. Refresh and try again.";
					return;
				}

				if (draftResult.IsSuccess && draftResult.Payload is not null && draftResult.Payload.MissingRequiredFields.Count > 0)
				{
					IsEligible = false;
					ShowProfileCompletionAction = true;
					GuidanceMessage = "Profile is incomplete. Complete required fields to continue fast-path check-in.";
				}
			}

			ErrorMessage = null;
		}
		catch
		{
			ErrorMessage = EvaluateFastPathCheckInConstants.SafeFastPathUnavailableMessage;
		}
		finally
		{
			IsLoading = false;
		}
	}
}
