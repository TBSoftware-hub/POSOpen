using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Domain.Policies;

/// <summary>Blocks checkout when the cart has no line items.</summary>
public sealed class CartMustHaveItemsRule : ICartCompatibilityRule
{
	public IReadOnlyList<CartValidationIssue> Evaluate(CartSession cart)
	{
		if (cart.LineItems.Count == 0)
		{
			return
			[
				new CartValidationIssue(
					Code: "CART_EMPTY",
					Severity: ValidationSeverity.Blocking,
					Message: "The cart is empty. Add at least one item to proceed.",
					FixLabel: null,
					FixAction: CartValidationFixAction.None)
			];
		}

		return [];
	}
}
