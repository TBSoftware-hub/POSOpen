using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Checkout;
using POSOpen.Domain.Enums;
using System.Collections.ObjectModel;

namespace POSOpen.Features.Checkout.ViewModels;

public partial class RefundWorkflowViewModel : ObservableObject
{
	private readonly GetRefundEligibilityUseCase _getRefundEligibilityUseCase;
	private readonly SubmitRefundUseCase _submitRefundUseCase;
	private readonly IOperationContextFactory _operationContextFactory;
	private readonly ICheckoutUiService _checkoutUiService;

	private Guid? _cartSessionId;
	private long _eligibleAmountCents;

	public RefundWorkflowViewModel(
		GetRefundEligibilityUseCase getRefundEligibilityUseCase,
		SubmitRefundUseCase submitRefundUseCase,
		IOperationContextFactory operationContextFactory,
		ICheckoutUiService checkoutUiService)
	{
		_getRefundEligibilityUseCase = getRefundEligibilityUseCase;
		_submitRefundUseCase = submitRefundUseCase;
		_operationContextFactory = operationContextFactory;
		_checkoutUiService = checkoutUiService;
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
	private string _eligibleAmountLabel = "$0.00";

	[ObservableProperty]
	private string _amountCentsInput = string.Empty;

	[ObservableProperty]
	private string _reason = string.Empty;

	[ObservableProperty]
	private RefundPath _selectedPath = RefundPath.Direct;

	[ObservableProperty]
	private bool _isEligible;

	[ObservableProperty]
	private bool _canUseDirectPath;

	[ObservableProperty]
	private bool _canUseApprovalPath;

	[ObservableProperty]
	private bool _isCompleted;

	[ObservableProperty]
	private bool _isPendingApproval;

	public ObservableCollection<RefundPath> AvailablePaths { get; } = [];

	public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

	public bool IsApprovalOnlyMode => CanUseApprovalPath && !CanUseDirectPath;

	public bool IsBlocked => !IsEligible && !HasError;

	[RelayCommand]
	private async Task InitializeAsync()
	{
		IsLoading = true;
		ErrorMessage = null;
		StatusMessage = string.Empty;
		IsCompleted = false;
		IsPendingApproval = false;

		try
		{
			if (!Guid.TryParse(CartSessionIdParam, out var cartId))
			{
				ErrorMessage = "Refund transaction reference is invalid.";
				return;
			}

			_cartSessionId = cartId;
			var result = await _getRefundEligibilityUseCase.ExecuteAsync(cartId);
			if (!result.IsSuccess || result.Payload is null)
			{
				ErrorMessage = result.UserMessage;
				return;
			}

			var eligibility = result.Payload;
			IsEligible = eligibility.IsEligible;
			StatusMessage = eligibility.UserMessage;
			_eligibleAmountCents = eligibility.EligibleAmountCents;
			EligibleAmountLabel = (_eligibleAmountCents / 100m).ToString("C");
			AmountCentsInput = _eligibleAmountCents > 0 ? _eligibleAmountCents.ToString() : string.Empty;

			CanUseDirectPath = eligibility.AllowedPaths.Contains(RefundPath.Direct);
			CanUseApprovalPath = eligibility.AllowedPaths.Contains(RefundPath.ApprovalRequired);
			AvailablePaths.Clear();
			foreach (var path in eligibility.AllowedPaths)
			{
				AvailablePaths.Add(path);
			}

			if (CanUseDirectPath)
			{
				SelectedPath = RefundPath.Direct;
			}
			else if (CanUseApprovalPath)
			{
				SelectedPath = RefundPath.ApprovalRequired;
			}
		}
		finally
		{
			IsLoading = false;
		}
	}

	[RelayCommand]
	private async Task SubmitAsync()
	{
		if (_cartSessionId is not { } cartSessionId || IsLoading)
		{
			return;
		}

		if (!long.TryParse(AmountCentsInput, out var amountCents))
		{
			ErrorMessage = "Enter refund amount as a whole number (cents). Example: 1500 = $15.00";
			return;
		}

		IsLoading = true;
		ErrorMessage = null;
		try
		{
			var operation = _operationContextFactory.CreateRoot();
			var command = new SubmitRefundCommand(
				cartSessionId,
				amountCents,
				Reason,
				SelectedPath,
				operation);

			var result = await _submitRefundUseCase.ExecuteAsync(command);
			if (!result.IsSuccess || result.Payload is null)
			{
				ErrorMessage = result.UserMessage;
				StatusMessage = result.UserMessage;
				return;
			}

			StatusMessage = result.UserMessage;
			IsCompleted = result.Payload.Status == RefundStatus.Completed;
			IsPendingApproval = result.Payload.Status == RefundStatus.PendingApproval;
		}
		finally
		{
			IsLoading = false;
		}
	}

	[RelayCommand]
	private Task BackToCompletionAsync()
	{
		if (_cartSessionId is not { } cartSessionId)
		{
			return Task.CompletedTask;
		}

		return _checkoutUiService.NavigateToCheckoutCompletionAsync(cartSessionId);
	}
}