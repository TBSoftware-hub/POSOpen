namespace POSOpen.Domain.Enums;

/// <summary>
/// Identifies the automated fix action the ViewModel should apply
/// when a cashier taps a suggested-fix button.
/// </summary>
public enum CartValidationFixAction
{
	/// <summary>No automated fix; informational issue only.</summary>
	None = 0,

	/// <summary>Remove all CateringAddon line items from the cart.</summary>
	RemoveCateringItems = 1,

	/// <summary>
	/// Remove all but the first (oldest) PartyDeposit line item.
	/// Items are already ordered by CreatedAtUtc in the DTO, so Skip(1) is safe.
	/// </summary>
	KeepOldestPartyDeposit = 2,
}
