using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Domain.Policies;

/// <summary>
/// Blocks checkout when the cart contains more than one PartyDeposit line item.
/// </summary>
public sealed class SinglePartyDepositRule : ICartCompatibilityRule
{
	public IReadOnlyList<CartValidationIssue> Evaluate(CartSession cart)
	{
		int depositCount = cart.LineItems.Count(i => i.FulfillmentContext == FulfillmentContext.PartyDeposit);

		if (depositCount > 1)
		{
			return
			[
				new CartValidationIssue(
					Code: "MULTIPLE_PARTY_DEPOSITS",
					Severity: ValidationSeverity.Blocking,
					Message: "Only one party deposit is allowed per cart.",
					FixLabel: "Keep first deposit, remove extras",
					FixAction: CartValidationFixAction.KeepOldestPartyDeposit)
			];
		}

		return [];
	}
}
