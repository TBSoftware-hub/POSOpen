using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Checkout;
using POSOpen.Domain.Enums;

namespace POSOpen.Features.Checkout.ViewModels;

public partial class CheckoutCompletionViewModel : ObservableObject
{
	private readonly PrintReceiptUseCase _printReceiptUseCase;
	private readonly GetTransactionStatusUseCase _getTransactionStatusUseCase;
	private readonly ICheckoutUiService _uiService;
	private readonly ILogger<CheckoutCompletionViewModel> _logger;

	private Guid? _cartSessionId;

	public CheckoutCompletionViewModel(
		PrintReceiptUseCase printReceiptUseCase,
		GetTransactionStatusUseCase getTransactionStatusUseCase,
		ICheckoutUiService uiService,
		ILogger<CheckoutCompletionViewModel> logger)
	{
		_printReceiptUseCase = printReceiptUseCase;
		_getTransactionStatusUseCase = getTransactionStatusUseCase;
		_uiService = uiService;
		_logger = logger;
	}

	public string CartSessionIdParam { get; set; } = string.Empty;

	[ObservableProperty]
	private bool _isLoading;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasError))]
	private string? _errorMessage;

	[ObservableProperty]
	private string _statusMessage = string.Empty;

	[ObservableProperty]
	private string _nextStepsMessage = string.Empty;

	[ObservableProperty]
	private string _receiptStatusMessage = string.Empty;

	[ObservableProperty]
	private string? _operationIdReference;

	public bool HasOperationReference => !string.IsNullOrWhiteSpace(OperationIdReference);

	public bool CanStartRefund => _cartSessionId.HasValue;

	[ObservableProperty]
	private bool _isOnlineCompletion;

	[ObservableProperty]
	private bool _isPrintDeferred;

	public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

	[RelayCommand]
	private Task NewTransactionAsync() => _uiService.StartNewTransactionAsync();

	[RelayCommand]
	private Task StartRefundAsync()
	{
		if (_cartSessionId is not { } cartSessionId)
		{
			return Task.CompletedTask;
		}

		return _uiService.NavigateToRefundWorkflowAsync(cartSessionId);
	}

	[RelayCommand]
	private async Task InitializeAsync()
	{
		IsLoading = true;
		ErrorMessage = null;
		_cartSessionId = null;
		OnPropertyChanged(nameof(CanStartRefund));

		try
		{
			if (!Guid.TryParse(CartSessionIdParam, out var cartId))
			{
				ErrorMessage = "Checkout session reference is invalid.";
				return;
			}

			_cartSessionId = cartId;
			OnPropertyChanged(nameof(CanStartRefund));

			var statusResult = await _getTransactionStatusUseCase.ExecuteAsync(cartId);
			if (!statusResult.IsSuccess || statusResult.Payload is null)
			{
				ErrorMessage = statusResult.UserMessage;
				return;
			}

			var status = statusResult.Payload;
			StatusMessage = status.StatusMessage;
			NextStepsMessage = status.NextStepsMessage;
			IsOnlineCompletion = status.TransactionStatus == TransactionStatus.CompletedOnline;

			if (status.LastOperationId.HasValue)
				OperationIdReference = status.LastOperationId.Value.ToString();

			var printResult = await _printReceiptUseCase.ExecuteAsync(cartId);
			if (!printResult.IsSuccess || printResult.Payload is null)
			{
				ReceiptStatusMessage = printResult.UserMessage;
				IsPrintDeferred = true;
				_logger.LogWarning("Print receipt failed for cart {CartId}: {UserMessage}", cartId, printResult.UserMessage);
				return;
			}

			var receipt = printResult.Payload;
			OperationIdReference = receipt.OperationId.ToString();
			ReceiptStatusMessage = receipt.UserMessage;
			IsPrintDeferred = receipt.PrintStatus != PrintStatus.Success;

			if (receipt.DiagnosticCode is not null)
				_logger.LogInformation("Receipt print completed for cart {CartId} with status {PrintStatus} diagnostic {DiagnosticCode}",
					cartId, receipt.PrintStatus, receipt.DiagnosticCode);
		}
		finally
		{
			IsLoading = false;
		}
	}
}
