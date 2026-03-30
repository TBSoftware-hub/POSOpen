using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Admissions;
using POSOpen.Domain.Enums;
using System.Globalization;

namespace POSOpen.Features.Admissions.ViewModels;

public partial class FastPathCheckInViewModel : ObservableObject
{
	private const double FeedbackLatencyThresholdMilliseconds = 2000;
	private const string EvaluateInteractionName = "EvaluateFastPathCheckIn";

	private readonly EvaluateFastPathCheckInUseCase _evaluateFastPathCheckInUseCase;
	private readonly ProfileAdmissionUseCase _profileAdmissionUseCase;
	private readonly CompleteAdmissionCheckInUseCase _completeAdmissionCheckInUseCase;
	private readonly IAdmissionPricingService _admissionPricingService;
	private readonly IFastPathCheckInUiService _uiService;
	private readonly ICheckInLatencyTimer _checkInLatencyTimer;
	private readonly ICheckInLatencyMonitor _checkInLatencyMonitor;

	public FastPathCheckInViewModel(
		EvaluateFastPathCheckInUseCase evaluateFastPathCheckInUseCase,
		ProfileAdmissionUseCase profileAdmissionUseCase,
		CompleteAdmissionCheckInUseCase completeAdmissionCheckInUseCase,
		IAdmissionPricingService admissionPricingService,
		IFastPathCheckInUiService uiService,
		ICheckInLatencyTimer checkInLatencyTimer,
		ICheckInLatencyMonitor checkInLatencyMonitor)
	{
		_evaluateFastPathCheckInUseCase = evaluateFastPathCheckInUseCase;
		_profileAdmissionUseCase = profileAdmissionUseCase;
		_completeAdmissionCheckInUseCase = completeAdmissionCheckInUseCase;
		_admissionPricingService = admissionPricingService;
		_uiService = uiService;
		_checkInLatencyTimer = checkInLatencyTimer;
		_checkInLatencyMonitor = checkInLatencyMonitor;
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
	private string _admissionTotalLabel = "$25.00 USD";

	[ObservableProperty]
	private bool _showCompletionResult;

	[ObservableProperty]
	private bool _isDeferredQueued;

	[ObservableProperty]
	private string _completionStatusLabel = string.Empty;

	[ObservableProperty]
	private string _completionGuidance = string.Empty;

	[ObservableProperty]
	private string _confirmationCode = string.Empty;

	[ObservableProperty]
	private string _receiptReference = string.Empty;

	[ObservableProperty]
	private string _operationIdText = string.Empty;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasError))]
	private string? _errorMessage;

	public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

	public void Initialize(Guid familyId)
	{
		FamilyId = familyId;
		ResetCompletionState();
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
	private async Task CompleteCheckInAsync(CancellationToken ct = default)
	{
		await EvaluateAsync(isRefreshRequested: true, ct);
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
			IsLoading = true;

			var total = await _admissionPricingService.GetAdmissionTotalAsync(FamilyId!.Value, ct);
			var result = await _completeAdmissionCheckInUseCase.ExecuteAsync(
				new CompleteAdmissionCheckInCommand(FamilyId.Value, total.AmountCents, total.CurrencyCode),
				ct);

			if (!result.IsSuccess || result.Payload is null)
			{
				ErrorMessage = result.UserMessage;
				ShowCompletionResult = false;
				return;
			}

			ApplyCompletionState(result.Payload);
		}
		catch
		{
			ErrorMessage = CompleteAdmissionCheckInConstants.SafeCompletionFailedMessage;
		}
		finally
		{
			IsLoading = false;
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
		var startTimestamp = _checkInLatencyTimer.GetTimestamp();
		var latencyCaptured = false;

		void CaptureLatency()
		{
			if (latencyCaptured)
			{
				return;
			}

			var elapsedMilliseconds = _checkInLatencyTimer.GetElapsedMilliseconds(
				startTimestamp,
				_checkInLatencyTimer.GetTimestamp());

			_checkInLatencyMonitor.Record(
				EvaluateInteractionName,
				FamilyId,
				elapsedMilliseconds,
				elapsedMilliseconds > FeedbackLatencyThresholdMilliseconds);

			latencyCaptured = true;
		}

		if (FamilyId is null)
		{
			CaptureLatency();
			ErrorMessage = "Select a family before starting fast-path check-in.";
			return;
		}

		IsLoading = true;
		ResetCompletionState();
		try
		{
			var result = await _evaluateFastPathCheckInUseCase.ExecuteAsync(
				new EvaluateFastPathCheckInQuery(FamilyId.Value, isRefreshRequested),
				ct);

			if (!result.IsSuccess || result.Payload is null)
			{
				CaptureLatency();
				ErrorMessage = result.UserMessage;
				return;
			}

			var payload = result.Payload;
			CaptureLatency();
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
				var total = await _admissionPricingService.GetAdmissionTotalAsync(FamilyId.Value, ct);
				AdmissionTotalLabel = FormatTotal(total.AmountCents, total.CurrencyCode);

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
			CaptureLatency();
			ErrorMessage = EvaluateFastPathCheckInConstants.SafeFastPathUnavailableMessage;
		}
		finally
		{
			IsLoading = false;
		}
	}

	private void ApplyCompletionState(AdmissionCompletionResultDto result)
	{
		ShowCompletionResult = true;
		IsDeferredQueued = result.SettlementStatus == AdmissionSettlementStatus.DeferredQueued;
		CompletionStatusLabel = result.SettlementStatusLabel;
		CompletionGuidance = result.GuidanceMessage;
		ConfirmationCode = result.ConfirmationCode;
		ReceiptReference = result.ReceiptReference;
		OperationIdText = result.OperationId.ToString();
		GuidanceMessage = result.GuidanceMessage;
		ErrorMessage = null;
	}

	private static string FormatTotal(long amountCents, string currencyCode)
	{
		var amount = amountCents / 100m;
		return string.Format(
			CultureInfo.InvariantCulture,
			"{0:0.00} {1}",
			amount,
			currencyCode.Trim().ToUpperInvariant());
	}

	private void ResetCompletionState()
	{
		ShowCompletionResult = false;
		IsDeferredQueued = false;
		CompletionStatusLabel = string.Empty;
		CompletionGuidance = string.Empty;
		ConfirmationCode = string.Empty;
		ReceiptReference = string.Empty;
		OperationIdText = string.Empty;
	}
}
