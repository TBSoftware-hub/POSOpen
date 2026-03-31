using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Checkout;

namespace POSOpen.Features.Checkout.ViewModels;

public partial class PaymentCaptureViewModel : ObservableObject
{
	private readonly GetCartPaymentSummaryUseCase _getCartPaymentSummaryUseCase;
	private readonly ProcessCardPaymentUseCase _processCardPaymentUseCase;
	private readonly ICheckoutUiService _checkoutUiService;
	private Guid? _cartSessionId;

	public PaymentCaptureViewModel(
		GetCartPaymentSummaryUseCase getCartPaymentSummaryUseCase,
		ProcessCardPaymentUseCase processCardPaymentUseCase,
		ICheckoutUiService checkoutUiService)
	{
		_getCartPaymentSummaryUseCase = getCartPaymentSummaryUseCase;
		_processCardPaymentUseCase = processCardPaymentUseCase;
		_checkoutUiService = checkoutUiService;
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

	public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

	public bool HasDiagnosticCode => !string.IsNullOrWhiteSpace(DiagnosticCode);

	public bool HasProcessorReference => !string.IsNullOrWhiteSpace(ProcessorReference);

	[RelayCommand]
	private async Task InitializeAsync()
	{
		IsLoading = true;
		ErrorMessage = null;
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
}