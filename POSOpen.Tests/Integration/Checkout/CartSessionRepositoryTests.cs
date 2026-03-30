using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Infrastructure.Persistence;
using POSOpen.Infrastructure.Persistence.Repositories;

namespace POSOpen.Tests.Integration.Checkout;

public sealed class CartSessionRepositoryTests
{
	[Fact]
	public async Task CreateAsync_followed_by_GetByIdAsync_returns_persisted_cart()
	{
		await using var fixture = await CreateFixtureAsync();
		var staffId = Guid.NewGuid();
		var createdAt = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
		var cart = CartSession.Create(Guid.NewGuid(), null, staffId, createdAt);

		await fixture.Repository.CreateAsync(cart);
		var retrieved = await fixture.Repository.GetByIdAsync(cart.Id);

		retrieved.Should().NotBeNull();
		retrieved!.Id.Should().Be(cart.Id);
		retrieved.StaffId.Should().Be(staffId);
		retrieved.Status.Should().Be(CartStatus.Open);
		retrieved.CreatedAtUtc.Should().Be(createdAt);
		retrieved.LineItems.Should().BeEmpty();
	}

	[Fact]
	public async Task GetOpenCartForStaffAsync_returns_cart_for_matching_staff()
	{
		await using var fixture = await CreateFixtureAsync();
		var staffId = Guid.NewGuid();
		var cart = CartSession.Create(Guid.NewGuid(), null, staffId, DateTime.UtcNow);
		await fixture.Repository.CreateAsync(cart);

		var result = await fixture.Repository.GetOpenCartForStaffAsync(staffId);

		result.Should().NotBeNull();
		result!.Id.Should().Be(cart.Id);
		result.StaffId.Should().Be(staffId);
	}

	[Fact]
	public async Task GetOpenCartForStaffAsync_returns_null_when_no_open_cart_exists()
	{
		await using var fixture = await CreateFixtureAsync();
		var result = await fixture.Repository.GetOpenCartForStaffAsync(Guid.NewGuid());
		result.Should().BeNull();
	}

	[Fact]
	public async Task AddLineItemAsync_persists_item_and_includes_in_retrieved_cart()
	{
		await using var fixture = await CreateFixtureAsync();
		var now = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
		var cart = CartSession.Create(Guid.NewGuid(), null, Guid.NewGuid(), now);
		await fixture.Repository.CreateAsync(cart);

		var lineItem = CartLineItem.Create(
			Guid.NewGuid(), cart.Id, "General Admission",
			FulfillmentContext.Admission, null, 2, 1500, "USD", now);

		var updated = await fixture.Repository.AddLineItemAsync(cart.Id, lineItem, now);

		updated.Should().NotBeNull();
		updated!.LineItems.Should().ContainSingle();
		var persistedItem = updated.LineItems.Single();
		persistedItem.Description.Should().Be("General Admission");
		persistedItem.Quantity.Should().Be(2);
		persistedItem.UnitAmountCents.Should().Be(1500);
	}

	[Fact]
	public async Task RemoveLineItemAsync_removes_item_from_persisted_cart()
	{
		await using var fixture = await CreateFixtureAsync();
		var now = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
		var cart = CartSession.Create(Guid.NewGuid(), null, Guid.NewGuid(), now);
		await fixture.Repository.CreateAsync(cart);

		var lineItemId = Guid.NewGuid();
		var lineItem = CartLineItem.Create(lineItemId, cart.Id, "T-Shirt", FulfillmentContext.RetailItem, null, 1, 2500, "USD", now);
		await fixture.Repository.AddLineItemAsync(cart.Id, lineItem, now);

		var removedAt = now.AddMinutes(1);
		var result = await fixture.Repository.RemoveLineItemAsync(cart.Id, lineItemId, removedAt);

		result.Should().NotBeNull();
		result!.LineItems.Should().BeEmpty();
	}

	[Fact]
	public async Task UpdateLineItemQuantityAsync_persists_new_quantity()
	{
		await using var fixture = await CreateFixtureAsync();
		var now = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
		var cart = CartSession.Create(Guid.NewGuid(), null, Guid.NewGuid(), now);
		await fixture.Repository.CreateAsync(cart);

		var lineItemId = Guid.NewGuid();
		var lineItem = CartLineItem.Create(lineItemId, cart.Id, "Party Deposit", FulfillmentContext.PartyDeposit, null, 1, 5000, "USD", now);
		await fixture.Repository.AddLineItemAsync(cart.Id, lineItem, now);

		var updatedAt = now.AddMinutes(2);
		var result = await fixture.Repository.UpdateLineItemQuantityAsync(cart.Id, lineItemId, 3, updatedAt);

		result.Should().NotBeNull();
		result!.LineItems.Should().ContainSingle();
		var updatedItem = result.LineItems.Single();
		updatedItem.Quantity.Should().Be(3);
		updatedItem.UpdatedAtUtc.Should().Be(updatedAt);
	}

	[Fact]
	public async Task RemoveLineItemAsync_returns_null_when_cart_not_found()
	{
		await using var fixture = await CreateFixtureAsync();
		var result = await fixture.Repository.RemoveLineItemAsync(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
		result.Should().BeNull();
	}

	// ─── Helpers ─────────────────────────────────────────────────────────

	private static async Task<TestFixture> CreateFixtureAsync()
	{
		var databasePath = TestDatabasePaths.Create();
		var dbContextFactory = new TestDbContextFactory(databasePath);
		var initializer = new AppDbContextInitializer(dbContextFactory, NullLogger<AppDbContextInitializer>.Instance);
		await initializer.InitializeAsync();
		return new TestFixture(dbContextFactory, new CartSessionRepository(dbContextFactory));
	}

	private sealed class TestFixture : IAsyncDisposable
	{
		public TestFixture(TestDbContextFactory dbContextFactory, CartSessionRepository repository)
		{
			DbContextFactory = dbContextFactory;
			Repository = repository;
		}

		public TestDbContextFactory DbContextFactory { get; }
		public CartSessionRepository Repository { get; }

		public async ValueTask DisposeAsync()
		{
			await DbContextFactory.DisposeAsync();
		}
	}
}
