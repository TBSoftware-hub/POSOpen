using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Checkout;
using POSOpen.Domain.Enums;

namespace POSOpen.Features.Checkout.ViewModels;

public partial class CartViewModel : ObservableObject
{
	private static readonly (FulfillmentContext Context, string Name, string Icon)[] GroupDefinitions =
	[
		(FulfillmentContext.Admission, "Admissions", "🎟"),
		(FulfillmentContext.RetailItem, "Retail", "🛍"),
		(FulfillmentContext.PartyDeposit, "Party Deposit", "🎉"),
		(FulfillmentContext.CateringAddon, "Catering Add-ons", "🍽"),
	];

	private readonly GetOrCreateCartSessionUseCase _getOrCreateCartSession;
	private readonly CaptureScannerInputUseCase _captureScannerInputUseCase;
	private readonly RemoveCartLineItemUseCase _removeCartLineItem;
	private readonly UpdateCartLineItemQuantityUseCase _updateCartLineItemQuantity;
	private readonly ValidateCartCompatibilityUseCase _validateCartCompatibility;
	private readonly PrintReceiptUseCase _printReceiptUseCase;
	private readonly ICheckoutUiService _uiService;

	private Guid? _cartSessionId;
	private Guid? _highlightedLineItemId;

	public CartViewModel(
		GetOrCreateCartSessionUseCase getOrCreateCartSession,
		CaptureScannerInputUseCase captureScannerInputUseCase,
		RemoveCartLineItemUseCase removeCartLineItem,
		UpdateCartLineItemQuantityUseCase updateCartLineItemQuantity,
		ValidateCartCompatibilityUseCase validateCartCompatibility,
		PrintReceiptUseCase printReceiptUseCase,
		ICheckoutUiService uiService)
	{
		_getOrCreateCartSession = getOrCreateCartSession;
		_captureScannerInputUseCase = captureScannerInputUseCase;
		_removeCartLineItem = removeCartLineItem;
		_updateCartLineItemQuantity = updateCartLineItemQuantity;
		_validateCartCompatibility = validateCartCompatibility;
		_printReceiptUseCase = printReceiptUseCase;
		_uiService = uiService;
	}

	[ObservableProperty]
	private bool _isLoading;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasError))]
	private string? _errorMessage;

	[ObservableProperty]
	private string _grandTotalLabel = "$0.00";

	[ObservableProperty]
	private bool _isCartValid;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasOfflineStatus))]
	private PrintStatus? _lastPrintStatus;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasOfflineStatus))]
	private string? _offlineStatusMessage;

	public bool HasOfflineStatus => !string.IsNullOrWhiteSpace(OfflineStatusMessage);

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasScannerStatus))]
	private string? _scannerStatusMessage;

	public ObservableCollection<CartLineItemGroupViewModel> ItemGroups { get; } = new();

	public ObservableCollection<ValidationIssueViewModel> ValidationIssues { get; } = [];

	public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

	public bool HasValidationIssues => ValidationIssues.Count > 0;

	public bool HasScannerStatus => !string.IsNullOrWhiteSpace(ScannerStatusMessage);

	[RelayCommand]
	private async Task InitializeAsync()
	{
		IsLoading = true;
		ErrorMessage = null;
		_cartSessionId = null;
		ItemGroups.Clear();
		GrandTotalLabel = "$0.00";
		try
		{
			var result = await _getOrCreateCartSession.ExecuteAsync();
			if (!result.IsSuccess)
			{
				ErrorMessage = result.UserMessage;
				return;
			}
			_cartSessionId = result.Payload!.Id;
			_highlightedLineItemId = null;
			ScannerStatusMessage = null;
			RefreshGroupsFromDto(result.Payload!, null);
			await RunValidationAsync();
		}
		finally
		{
			IsLoading = false;
		}
	}

	[RelayCommand]
	private async Task RemoveItemAsync(Guid lineItemId)
	{
		if (_cartSessionId is not { } cartId) return;

		var result = await _removeCartLineItem.ExecuteAsync(
			new RemoveCartLineItemCommand(cartId, lineItemId));

		if (result.IsSuccess)
		{
			ErrorMessage = null;
			_highlightedLineItemId = null;
			RefreshGroupsFromDto(result.Payload!, null);
			await RunValidationAsync();
		}
		else
			ErrorMessage = result.UserMessage;
	}

	[RelayCommand]
	private async Task IncrementQuantityAsync(Guid lineItemId)
	{
		if (_cartSessionId is not { } cartId) return;

		var current = ItemGroups.SelectMany(g => g).FirstOrDefault(i => i.Id == lineItemId);
		if (current is null) return;

		var result = await _updateCartLineItemQuantity.ExecuteAsync(
			new UpdateCartLineItemQuantityCommand(cartId, lineItemId, current.Quantity + 1));

		if (result.IsSuccess)
		{
			ErrorMessage = null;
			_highlightedLineItemId = null;
			RefreshGroupsFromDto(result.Payload!, null);
			await RunValidationAsync();
		}
		else
			ErrorMessage = result.UserMessage;
	}

	[RelayCommand]
	private async Task DecrementQuantityAsync(Guid lineItemId)
	{
		if (_cartSessionId is not { } cartId) return;

		var current = ItemGroups.SelectMany(g => g).FirstOrDefault(i => i.Id == lineItemId);
		if (current is null) return;

		if (current.Quantity <= 1)
		{
			await RemoveItemAsync(lineItemId);
			return;
		}

		var result = await _updateCartLineItemQuantity.ExecuteAsync(
			new UpdateCartLineItemQuantityCommand(cartId, lineItemId, current.Quantity - 1));

		if (result.IsSuccess)
		{
			ErrorMessage = null;
			_highlightedLineItemId = null;
			RefreshGroupsFromDto(result.Payload!, null);
			await RunValidationAsync();
		}
		else
			ErrorMessage = result.UserMessage;
	}

	[RelayCommand]
	private async Task NavigateToAddLineItemAsync()
	{
		if (_cartSessionId is null) return;
		await _uiService.NavigateToAddLineItemAsync(_cartSessionId.Value);
	}

	[RelayCommand]
	private async Task CaptureScanAsync()
	{
		if (_cartSessionId is not { } cartId)
		{
			return;
		}

		var result = await _captureScannerInputUseCase.ExecuteAsync(cartId);
		if (!result.IsSuccess || result.Payload is null)
		{
			ErrorMessage = result.UserMessage;
			return;
		}

		ErrorMessage = null;
		ScannerStatusMessage = result.Payload.UserMessage;
		_highlightedLineItemId = result.Payload.SelectedLineItemId;
		RefreshGroupsFromDto(result.Payload.Cart, _highlightedLineItemId);
	}

	private async Task RunValidationAsync()
	{
		if (_cartSessionId is not { } cartId)
		{
			IsCartValid = false;
			ValidationIssues.Clear();
			OnPropertyChanged(nameof(HasValidationIssues));
			return;
		}

		var result = await _validateCartCompatibility.ExecuteAsync(cartId);

		if (!result.IsSuccess)
		{
			// Validation infrastructure failure — treat cart as invalid but don't
			// surface internal error codes to cashier.
			IsCartValid = false;
			ValidationIssues.Clear();
			OnPropertyChanged(nameof(HasValidationIssues));
			return;
		}

		ValidationIssues.Clear();
		foreach (var issue in result.Payload!.Issues)
		{
			ValidationIssues.Add(new ValidationIssueViewModel
			{
				Message   = issue.Message,
				FixLabel  = issue.FixLabel,
				FixAction = issue.FixAction,
			});
		}

		IsCartValid = result.Payload.IsValid;
		OnPropertyChanged(nameof(HasValidationIssues));
	}

	[RelayCommand]
	private async Task ApplyFixAsync(CartValidationFixAction action)
	{
		if (_cartSessionId is not { } cartId) return;

		switch (action)
		{
			case CartValidationFixAction.RemoveCateringItems:
				var cateringIds = ItemGroups
					.SelectMany(g => g)
					.Where(i => i.FulfillmentContext == FulfillmentContext.CateringAddon)
					.Select(i => i.Id)
					.ToList();
				foreach (var id in cateringIds)
				{
					var removeResult = await _removeCartLineItem.ExecuteAsync(
						new RemoveCartLineItemCommand(cartId, id));
					if (!removeResult.IsSuccess)
					{
						ErrorMessage = removeResult.UserMessage;
						break;
					}
				}
				await RefreshAndValidateAsync();
				break;

			case CartValidationFixAction.KeepOldestPartyDeposit:
				var extraDepositIds = ItemGroups
					.SelectMany(g => g)
					.Where(i => i.FulfillmentContext == FulfillmentContext.PartyDeposit)
					.Skip(1)
					.Select(i => i.Id)
					.ToList();
				foreach (var id in extraDepositIds)
				{
					var removeResult = await _removeCartLineItem.ExecuteAsync(
						new RemoveCartLineItemCommand(cartId, id));
					if (!removeResult.IsSuccess)
					{
						ErrorMessage = removeResult.UserMessage;
						break;
					}
				}
				await RefreshAndValidateAsync();
				break;

			case CartValidationFixAction.None:
			default:
				break;
		}
	}

	private async Task RefreshAndValidateAsync()
	{
		var refreshResult = await _getOrCreateCartSession.ExecuteAsync();
		if (refreshResult.IsSuccess)
			RefreshGroupsFromDto(refreshResult.Payload!, _highlightedLineItemId);
		await RunValidationAsync();
	}

	[RelayCommand]
	private async Task ProceedToPaymentAsync()
	{
		if (_cartSessionId is not { } cartId || !IsCartValid)
		{
			return;
		}

		await _uiService.NavigateToPaymentCaptureAsync(cartId);
	}

	private void RefreshGroupsFromDto(CartSessionDto dto, Guid? highlightedLineItemId)
		[RelayCommand]
		private async Task PrintReceiptAsync()
		{
			if (_cartSessionId is not { } cartId) return;

			IsLoading = true;
			try
			{
				var result = await _printReceiptUseCase.ExecuteAsync(cartId);
				if (!result.IsSuccess || result.Payload is null)
				{
					OfflineStatusMessage = result.UserMessage;
					LastPrintStatus = null;
					return;
				}

				LastPrintStatus = result.Payload.PrintStatus;
				OfflineStatusMessage = result.Payload.UserMessage;
			}
			finally
			{
				IsLoading = false;
			}
		}

		private void RefreshGroupsFromDto(CartSessionDto dto, Guid? highlightedLineItemId)
	{
		ItemGroups.Clear();

		foreach (var (ctx, name, icon) in GroupDefinitions)
		{
			var items = dto.LineItems
				.Where(li => li.FulfillmentContext == ctx)
				.Select(li => new CartLineItemViewModel
				{
					Id = li.Id,
					CartSessionId = li.CartSessionId,
					Description = li.Description,
					FulfillmentContext = li.FulfillmentContext,
					ReferenceId = li.ReferenceId,
					Quantity = li.Quantity,
					UnitAmountCents = li.UnitAmountCents,
					LineTotalCents = li.LineTotalCents,
					CurrencyCode = li.CurrencyCode,
					IsHighlighted = li.Id == highlightedLineItemId,
				})
				.ToList();

			if (items.Count > 0)
				ItemGroups.Add(new CartLineItemGroupViewModel(name, icon, ctx, items));
		}

		GrandTotalLabel = (dto.TotalAmountCents / 100m).ToString("C", CultureInfo.CurrentCulture);
	}
}
