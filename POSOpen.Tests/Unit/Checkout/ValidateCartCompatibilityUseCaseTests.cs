using FluentAssertions;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.UseCases.Checkout;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Domain.Policies;

namespace POSOpen.Tests.Unit.Checkout;

public sealed class ValidateCartCompatibilityUseCaseTests
{
	private static readonly Guid StaffId = Guid.NewGuid();
	private static readonly DateTime FixedNow = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);

	private static CartSession EmptyCart() =>
		CartSession.Create(Guid.NewGuid(), null, StaffId, FixedNow);

	private static Mock<ICartSessionRepository> MockRepo(CartSession? cart = null)
	{
		var mock = new Mock<ICartSessionRepository>();
		mock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);
		return mock;
	}

	[Fact]
	public async Task CartNotFound_ReturnsFailure()
	{
		var repo = MockRepo(null);
		var sut = new ValidateCartCompatibilityUseCase(repo.Object, []);

		var result = await sut.ExecuteAsync(Guid.NewGuid());

		result.IsSuccess.Should().BeFalse();
	}

	[Fact]
	public async Task EmptyCart_IsValidFalse_WithCartEmptyIssue()
	{
		var cart = EmptyCart();
		var repo = MockRepo(cart);
		var sut = new ValidateCartCompatibilityUseCase(repo.Object, [new CartMustHaveItemsRule()]);

		var result = await sut.ExecuteAsync(cart.Id);

		result.IsSuccess.Should().BeTrue();
		result.Payload!.IsValid.Should().BeFalse();
		result.Payload.Issues.Should().ContainSingle(i => i.Code == "CART_EMPTY");
	}

	[Fact]
	public async Task CartWithAdmission_NoRuleViolations_IsValidTrue()
	{
		var cart = EmptyCart();
		cart.LineItems.Add(CartLineItem.Create(
			Guid.NewGuid(), cart.Id, "Ticket", FulfillmentContext.Admission, null, 1, 1500, "USD", FixedNow));
		var repo = MockRepo(cart);
		ICartCompatibilityRule[] rules =
		[
			new CartMustHaveItemsRule(),
			new CateringRequiresPartyDepositRule(),
			new SinglePartyDepositRule(),
		];
		var sut = new ValidateCartCompatibilityUseCase(repo.Object, rules);

		var result = await sut.ExecuteAsync(cart.Id);

		result.IsSuccess.Should().BeTrue();
		result.Payload!.IsValid.Should().BeTrue();
		result.Payload.Issues.Should().BeEmpty();
	}

	[Fact]
	public async Task TwoDepositsWithCatering_OnlyMultipleDepositsViolationFires()
	{
		var cart = EmptyCart();
		cart.LineItems.Add(CartLineItem.Create(Guid.NewGuid(), cart.Id, "Deposit 1", FulfillmentContext.PartyDeposit, null, 1, 5000, "USD", FixedNow));
		cart.LineItems.Add(CartLineItem.Create(Guid.NewGuid(), cart.Id, "Deposit 2", FulfillmentContext.PartyDeposit, null, 1, 5000, "USD", FixedNow));
		cart.LineItems.Add(CartLineItem.Create(Guid.NewGuid(), cart.Id, "Catering", FulfillmentContext.CateringAddon, null, 1, 500, "USD", FixedNow));
		var repo = MockRepo(cart);
		ICartCompatibilityRule[] rules =
		[
			new CartMustHaveItemsRule(),
			new CateringRequiresPartyDepositRule(),
			new SinglePartyDepositRule(),
		];
		var sut = new ValidateCartCompatibilityUseCase(repo.Object, rules);

		var result = await sut.ExecuteAsync(cart.Id);

		result.IsSuccess.Should().BeTrue();
		result.Payload!.IsValid.Should().BeFalse();
		result.Payload.Issues.Should().ContainSingle(i => i.Code == "MULTIPLE_PARTY_DEPOSITS");
	}
}
