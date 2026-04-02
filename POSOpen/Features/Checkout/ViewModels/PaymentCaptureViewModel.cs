using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POSOpen.Application;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Checkout;

namespace POSOpen.Features.Checkout.ViewModels;

public partial class PaymentCaptureViewModel : ObservableObject
{
	private readonly GetCartPaymentSummaryUseCase _getCartPaymentSummaryUseCase;
	private readonly ProcessCardPaymentUseCase _processCardPaymentUseCase;
	private readonly ICheckoutUiService _checkoutUiService;
	private readonly IConnectivityService _connectivityService;
	private readonly IWorkflowCapabilityService _workflowCapabilityService;
	private Guid? _cartSessionId;

	public PaymentCaptureViewModel(
		GetCartPaymentSummaryUseCase getCartPaymentSummaryUseCase,
		ProcessCardPaymentUseCase processCardPaymentUseCase,
		ICheckoutUiService checkoutUiService,
		IConnectivityService? connectivityService = null,
		IWorkflowCapabilityService? workflowCapabilityService = null)
	{
		_getCartPaymentSummaryUseCase = getCartPaymentSummaryUseCase;
		_processCardPaymentUseCase = processCardPaymentUseCase;
		_checkoutUiService = checkoutUiService;
		_connectivityService = connectivityService ?? AlwaysConnectedConnectivityService.Instance;
		_workflowCapabilityService = workflowCapabilityService ?? PassThroughWorkflowCapabilityService.Instance;
	}

	public string CartId { get; set; } = string.Empty;

	[ObservableProperty]
	private bool _isLoading;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasError))]
	private string? _errorMessage;

	[ObservableProperty]
	private string _amountLabel = "$0.00";

	[ObservableProperty]
	private string _statusMessage = "Ready to authorize payment.";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasDiagnosticCode))]
	private string? _diagnosticCode;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasProcessorReference))]
	private string? _processorReference;

	[ObservableProperty]
	private bool _isAuthorized;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasOfflineGuidance))]
	private string? _offlineGuidanceMessage;

	public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

	public bool HasDiagnosticCode => !string.IsNullOrWhiteSpace(DiagnosticCode);

	public bool HasProcessorReference => !string.IsNullOrWhiteSpace(ProcessorReference);

	public bool HasOfflineGuidance => !string.IsNullOrWhiteSpace(OfflineGuidanceMessage);

	[RelayCommand]
	private async Task InitializeAsync()
	{
		IsLoading = true;
		ErrorMessage = null;
		OfflineGuidanceMessage = ResolveOfflineGuidance();
		DiagnosticCode = null;
		ProcessorReference = null;
		IsAuthorized = false;

		try
		{
			if (!Guid.TryParse(CartId, out var cartSessionId))
			{
				ErrorMessage = "Payment session is invalid. Return to the cart and try again.";
				return;
			}

			_cartSessionId = cartSessionId;
			var summaryResult = await _getCartPaymentSummaryUseCase.ExecuteAsync(cartSessionId);
			if (!summaryResult.IsSuccess || summaryResult.Payload is null)
			{
				ErrorMessage = summaryResult.UserMessage;
				return;
			}

			AmountLabel = (summaryResult.Payload.AmountCents / 100m).ToString("C");
			StatusMessage = "Ready to authorize payment.";
		}
		finally
		{
			IsLoading = false;
		}
	}

	[RelayCommand]
	private async Task AuthorizeAsync()
	{
		if (_cartSessionId is not { } cartSessionId || IsLoading)
		{
			return;
		}

		OfflineGuidanceMessage = ResolveOfflineGuidance();
		if (!_connectivityService.IsConnected && !_workflowCapabilityService.IsOfflineSupported(WorkflowKeys.PaymentSettlement))
		{
			ErrorMessage = OfflineGuidanceMessage;
			StatusMessage = OfflineGuidanceMessage ?? "Payment authorization is unavailable while offline.";
			return;
		}

		IsLoading = true;
		ErrorMessage = null;
		try
		{
			var result = await _processCardPaymentUseCase.ExecuteAsync(cartSessionId);
			if (!result.IsSuccess || result.Payload is null)
			{
				ErrorMessage = result.UserMessage;
				StatusMessage = result.UserMessage;
				return;
			}

			var attempt = result.Payload.Attempt;
			DiagnosticCode = attempt.DiagnosticCode;
			ProcessorReference = attempt.ProcessorReference;
			IsAuthorized = result.Payload.IsAuthorized;
			StatusMessage = result.UserMessage;

			if (result.Payload.IsAuthorized && _cartSessionId.HasValue)
				await _checkoutUiService.NavigateToCheckoutCompletionAsync(_cartSessionId.Value);
		}
		finally
		{
			IsLoading = false;
		}
	}

	[RelayCommand]
	private async Task CancelAsync()
	{
		await _checkoutUiService.ClosePaymentCaptureAsync();
	}

	private string? ResolveOfflineGuidance()
	{
		if (_connectivityService.IsConnected)
		{
			return null;
		}

		if (!_workflowCapabilityService.IsOfflineSupported(WorkflowKeys.PaymentSettlement))
		{
			return _workflowCapabilityService.GetOfflineFallbackGuidance(WorkflowKeys.PaymentSettlement);
		}

		return null;
	}

	private sealed class AlwaysConnectedConnectivityService : IConnectivityService
	{
		public static readonly AlwaysConnectedConnectivityService Instance = new();

		public bool IsConnected => true;

		public event EventHandler<bool>? ConnectivityChanged
		{
			add { }
			remove { }
		}
	}

	private sealed class PassThroughWorkflowCapabilityService : IWorkflowCapabilityService
	{
		public static readonly PassThroughWorkflowCapabilityService Instance = new();

		public bool IsOfflineSupported(string workflowKey) => true;

		public string GetOfflineFallbackGuidance(string workflowKey) => string.Empty;
	}
}