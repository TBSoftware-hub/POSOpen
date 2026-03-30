using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using POSOpen.Application.UseCases.Checkout;
using POSOpen.Domain.Enums;

namespace POSOpen.Features.Checkout.ViewModels;

[QueryProperty(nameof(CartId), "cartId")]
public partial class AddLineItemViewModel : ObservableObject
{
	private readonly AddCartLineItemUseCase _addCartLineItem;

	public AddLineItemViewModel(AddCartLineItemUseCase addCartLineItem)
	{
		_addCartLineItem = addCartLineItem;
	}

	public string CartId { get; set; } = string.Empty;

	[ObservableProperty]
	private string _description = string.Empty;

	[ObservableProperty]
	private int _selectedFulfillmentContextIndex;

	[ObservableProperty]
	private string _quantityText = "1";

	[ObservableProperty]
	private string _unitPriceText = string.Empty;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasError))]
	private string? _errorMessage;

	public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

	public static IReadOnlyList<string> FulfillmentContextOptions { get; } =
	[
		"Admission",
		"Retail Item",
		"Party Deposit",
		"Catering Add-on",
	];

	[RelayCommand]
	private async Task ConfirmAddAsync()
	{
		ErrorMessage = null;

		if (string.IsNullOrWhiteSpace(Description))
		{
			ErrorMessage = "Description is required.";
			return;
		}

		if (!int.TryParse(QuantityText, out var quantity) || quantity < 1)
		{
			ErrorMessage = "Quantity must be a whole number of 1 or more.";
			return;
		}

		if (!decimal.TryParse(UnitPriceText, out var unitPriceDollars) || unitPriceDollars < 0)
		{
			ErrorMessage = "Unit price must be a valid amount (e.g. 12.50).";
			return;
		}

		if (!Guid.TryParse(CartId, out var cartSessionId))
		{
			ErrorMessage = "Cart session is invalid. Please go back and try again.";
			return;
		}

		var fulfillmentContext = (FulfillmentContext)SelectedFulfillmentContextIndex;
		var unitAmountCents = (long)Math.Round(unitPriceDollars * 100, MidpointRounding.AwayFromZero);

		var command = new AddCartLineItemCommand(
			cartSessionId,
			Description.Trim(),
			fulfillmentContext,
			ReferenceId: null,
			quantity,
			unitAmountCents);

		var result = await _addCartLineItem.ExecuteAsync(command);

		if (!result.IsSuccess)
		{
			ErrorMessage = result.UserMessage;
			return;
		}

		await Microsoft.Maui.Controls.Shell.Current.GoToAsync("..");
	}

	[RelayCommand]
	private async Task CancelAsync()
	{
		await Microsoft.Maui.Controls.Shell.Current.GoToAsync("..");
	}
}
