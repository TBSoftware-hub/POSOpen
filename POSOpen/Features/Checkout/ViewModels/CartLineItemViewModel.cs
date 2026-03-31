using System.Globalization;
using POSOpen.Domain.Enums;

namespace POSOpen.Features.Checkout.ViewModels;

public sealed class CartLineItemViewModel
{
	public Guid Id { get; init; }
	public Guid CartSessionId { get; init; }
	public string Description { get; init; } = string.Empty;
	public FulfillmentContext FulfillmentContext { get; init; }
	public Guid? ReferenceId { get; init; }
	public int Quantity { get; init; }
	public long UnitAmountCents { get; init; }
	public long LineTotalCents { get; init; }
	public string CurrencyCode { get; init; } = "USD";
	public bool IsHighlighted { get; init; }

	public string UnitPriceLabel => FormatCents(UnitAmountCents);
	public string LineTotalLabel => FormatCents(LineTotalCents);

	private static string FormatCents(long cents) =>
		(cents / 100m).ToString("C", CultureInfo.CurrentCulture);
}
