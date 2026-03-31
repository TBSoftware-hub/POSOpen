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
	private readonly RemoveCartLineItemUseCase _removeCartLineItem;
	private readonly UpdateCartLineItemQuantityUseCase _updateCartLineItemQuantity;
	private readonly ValidateCartCompatibilityUseCase _validateCartCompatibility;
	private readonly ICheckoutUiService _uiService;

	private Guid? _cartSessionId;

	public CartViewModel(
		GetOrCreateCartSessionUseCase getOrCreateCartSession,
		RemoveCartLineItemUseCase removeCartLineItem,
		UpdateCartLineItemQuantityUseCase updateCartLineItemQuantity,
		ValidateCartCompatibilityUseCase validateCartCompatibility,
		ICheckoutUiService uiService)
	{
		_getOrCreateCartSession = getOrCreateCartSession;
		_removeCartLineItem = removeCartLineItem;
		_updateCartLineItemQuantity = updateCartLineItemQuantity;
		_validateCartCompatibility = validateCartCompatibility;
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

	public ObservableCollection<CartLineItemGroupViewModel> ItemGroups { get; } = new();

	public ObservableCollection<ValidationIssueViewModel> ValidationIssues { get; } = [];

	public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

	public bool HasValidationIssues => ValidationIssues.Count > 0;

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
			RefreshGroupsFromDto(result.Payload!);
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
			RefreshGroupsFromDto(result.Payload!);
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
			RefreshGroupsFromDto(result.Payload!);
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
			RefreshGroupsFromDto(result.Payload!);
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
			RefreshGroupsFromDto(refreshResult.Payload!);
		await RunValidationAsync();
	}

	[RelayCommand]
	private async Task ProceedToPaymentAsync()
	{
		// TODO Story 3.3: navigate to payment capture page
		await Task.CompletedTask;
	}

	private void RefreshGroupsFromDto(CartSessionDto dto)
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
				})
				.ToList();

			if (items.Count > 0)
				ItemGroups.Add(new CartLineItemGroupViewModel(name, icon, ctx, items));
		}

		GrandTotalLabel = (dto.TotalAmountCents / 100m).ToString("C", CultureInfo.CurrentCulture);
	}
}
