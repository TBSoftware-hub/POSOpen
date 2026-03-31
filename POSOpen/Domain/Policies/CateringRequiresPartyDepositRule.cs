using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Domain.Policies;

/// <summary>
/// Blocks checkout when CateringAddon items exist in the cart but no
/// PartyDeposit item is present.
/// </summary>
public sealed class CateringRequiresPartyDepositRule : ICartCompatibilityRule
{
	public IReadOnlyList<CartValidationIssue> Evaluate(CartSession cart)
	{
		bool hasCatering     = cart.LineItems.Any(i => i.FulfillmentContext == FulfillmentContext.CateringAddon);
		bool hasPartyDeposit = cart.LineItems.Any(i => i.FulfillmentContext == FulfillmentContext.PartyDeposit);

		if (hasCatering && !hasPartyDeposit)
		{
			return
			[
				new CartValidationIssue(
					Code: "CATERING_WITHOUT_PARTY_DEPOSIT",
					Severity: ValidationSeverity.Blocking,
					Message: "Catering add-ons require a party deposit in the cart.",
					FixLabel: "Remove catering items",
					FixAction: CartValidationFixAction.RemoveCateringItems)
			];
		}

		return [];
	}
}
