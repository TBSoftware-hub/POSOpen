using System.Globalization;
using POSOpen.Domain.Enums;

namespace POSOpen.Features.Checkout.ViewModels;

/// <summary>
/// Represents a grouped set of cart line items for a single FulfillmentContext.
/// Inherits from List so MAUI's grouped CollectionView can use it directly.
/// </summary>
public sealed class CartLineItemGroupViewModel : List<CartLineItemViewModel>
{
	public string GroupName { get; }
	public string GroupIcon { get; }
	public FulfillmentContext Context { get; }
	public string SubtotalLabel { get; }

	public CartLineItemGroupViewModel(
		string groupName,
		string groupIcon,
		FulfillmentContext context,
		IEnumerable<CartLineItemViewModel> items)
		: base(items)
	{
		GroupName = groupName;
		GroupIcon = groupIcon;
		Context = context;
		var subtotalCents = this.Sum(i => i.LineTotalCents);
		SubtotalLabel = (subtotalCents / 100m).ToString("C", CultureInfo.CurrentCulture);
	}
}
