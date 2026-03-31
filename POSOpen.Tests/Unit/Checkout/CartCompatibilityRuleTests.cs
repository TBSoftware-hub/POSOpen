using FluentAssertions;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Domain.Policies;

namespace POSOpen.Tests.Unit.Checkout;

public sealed class CartCompatibilityRuleTests
{
	private static readonly Guid StaffId = Guid.NewGuid();
	private static readonly DateTime FixedNow = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);

	private static CartSession EmptyCart() =>
		CartSession.Create(Guid.NewGuid(), null, StaffId, FixedNow);

	private static CartSession CartWith(params FulfillmentContext[] contexts)
	{
		var cart = EmptyCart();
		foreach (var ctx in contexts)
			cart.LineItems.Add(CartLineItem.Create(
				Guid.NewGuid(), cart.Id, "Item", ctx, null, 1, 1000, "USD", FixedNow));
		return cart;
	}

	// ─── CartMustHaveItemsRule ────────────────────────────────────────────

	[Fact]
	public void CartMustHaveItemsRule_EmptyCart_ReturnsOneBlockingIssue()
	{
		var rule = new CartMustHaveItemsRule();
		var issues = rule.Evaluate(EmptyCart());

		issues.Should().ContainSingle()
			.Which.Code.Should().Be("CART_EMPTY");
		issues[0].Severity.Should().Be(ValidationSeverity.Blocking);
	}

	[Fact]
	public void CartMustHaveItemsRule_CartWithItems_ReturnsNoIssues()
	{
		var rule = new CartMustHaveItemsRule();
		var issues = rule.Evaluate(CartWith(FulfillmentContext.Admission));

		issues.Should().BeEmpty();
	}

	// ─── CateringRequiresPartyDepositRule ────────────────────────────────

	[Fact]
	public void CateringRequiresPartyDepositRule_CateringWithoutDeposit_ReturnsIssue()
	{
		var rule = new CateringRequiresPartyDepositRule();
		var issues = rule.Evaluate(CartWith(FulfillmentContext.CateringAddon));

		issues.Should().ContainSingle()
			.Which.Code.Should().Be("CATERING_WITHOUT_PARTY_DEPOSIT");
	}

	[Fact]
	public void CateringRequiresPartyDepositRule_CateringWithDeposit_ReturnsNoIssues()
	{
		var rule = new CateringRequiresPartyDepositRule();
		var issues = rule.Evaluate(CartWith(FulfillmentContext.CateringAddon, FulfillmentContext.PartyDeposit));

		issues.Should().BeEmpty();
	}

	[Fact]
	public void CateringRequiresPartyDepositRule_NoCatering_ReturnsNoIssues()
	{
		var rule = new CateringRequiresPartyDepositRule();
		var issues = rule.Evaluate(CartWith(FulfillmentContext.Admission));

		issues.Should().BeEmpty();
	}

	// ─── SinglePartyDepositRule ──────────────────────────────────────────

	[Fact]
	public void SinglePartyDepositRule_TwoDeposits_ReturnsIssue()
	{
		var rule = new SinglePartyDepositRule();
		var issues = rule.Evaluate(CartWith(FulfillmentContext.PartyDeposit, FulfillmentContext.PartyDeposit));

		issues.Should().ContainSingle()
			.Which.Code.Should().Be("MULTIPLE_PARTY_DEPOSITS");
	}

	[Fact]
	public void SinglePartyDepositRule_OneDeposit_ReturnsNoIssues()
	{
		var rule = new SinglePartyDepositRule();
		var issues = rule.Evaluate(CartWith(FulfillmentContext.PartyDeposit));

		issues.Should().BeEmpty();
	}

	[Fact]
	public void SinglePartyDepositRule_NoDeposit_ReturnsNoIssues()
	{
		var rule = new SinglePartyDepositRule();
		var issues = rule.Evaluate(CartWith(FulfillmentContext.Admission));

		issues.Should().BeEmpty();
	}
}
