using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Infrastructure.Persistence;
using POSOpen.Infrastructure.Persistence.Repositories;

namespace POSOpen.Tests.Integration.Admissions;

public sealed class FamilyProfileRepositoryTests
{
	[Fact]
	public async Task SearchAsync_matches_partial_last_name_case_insensitive()
	{
		await using var fixture = await CreateFixtureAsync();
		await fixture.Repository.AddAsync(CreateProfile("Alex", "Smith", "5551000", "token-smith"));
		await fixture.Repository.AddAsync(CreateProfile("Jordan", "Miles", "5552000", "token-miles"));

		var results = await fixture.Repository.SearchAsync("smi");

		results.Should().ContainSingle();
		results[0].PrimaryContactLastName.Should().Be("Smith");
	}

	[Fact]
	public async Task SearchAsync_matches_phone_substring()
	{
		await using var fixture = await CreateFixtureAsync();
		await fixture.Repository.AddAsync(CreateProfile("Casey", "Parker", "0412-555-100", "token-1"));
		await fixture.Repository.AddAsync(CreateProfile("Robin", "Parker", "0412-777-999", "token-2"));

		var results = await fixture.Repository.SearchAsync("555");

		results.Should().ContainSingle();
		results[0].Phone.Should().Contain("555");
	}

	[Fact]
	public async Task SearchAsync_returns_empty_when_no_records()
	{
		await using var fixture = await CreateFixtureAsync();
		var results = await fixture.Repository.SearchAsync("nobody");
		results.Should().BeEmpty();
	}

	[Fact]
	public async Task GetByScanTokenAsync_returns_exact_match()
	{
		await using var fixture = await CreateFixtureAsync();
		await fixture.Repository.AddAsync(CreateProfile("Pat", "Lane", "0400-111", "scan-abc"));

		var profile = await fixture.Repository.GetByScanTokenAsync("SCAN-ABC");

		profile.Should().NotBeNull();
		profile!.PrimaryContactLastName.Should().Be("Lane");
	}

	[Fact]
	public async Task GetByScanTokenAsync_trims_token_before_matching()
	{
		await using var fixture = await CreateFixtureAsync();
		await fixture.Repository.AddAsync(CreateProfile("Pat", "Lane", "0400-111", "scan-abc"));

		var profile = await fixture.Repository.GetByScanTokenAsync("  scan-abc  ");

		profile.Should().NotBeNull();
		profile!.PrimaryContactLastName.Should().Be("Lane");
	}

	[Fact]
	public async Task GetByScanTokenAsync_returns_null_for_unknown_token()
	{
		await using var fixture = await CreateFixtureAsync();
		await fixture.Repository.AddAsync(CreateProfile("Pat", "Lane", "0400-111", "scan-abc"));

		var profile = await fixture.Repository.GetByScanTokenAsync("missing-token");

		profile.Should().BeNull();
	}

	[Fact]
	public async Task AddAsync_followed_by_search_finds_new_profile()
	{
		await using var fixture = await CreateFixtureAsync();
		var profile = CreateProfile("Morgan", "Vale", "0499-123-111", "token-morgan");
		await fixture.Repository.AddAsync(profile);

		var results = await fixture.Repository.SearchAsync("vale");

		results.Should().ContainSingle(x => x.Id == profile.Id);
	}

	[Fact]
	public async Task SearchAsync_limits_results_to_twenty_and_orders_by_last_name()
	{
		await using var fixture = await CreateFixtureAsync();
		for (var i = 0; i < 25; i++)
		{
			await fixture.Repository.AddAsync(CreateProfile($"First{i:D2}", $"Family{i:D2}", $"0400-555-{i:D2}", $"token-{i:D2}"));
		}

		var results = await fixture.Repository.SearchAsync("family");

		results.Should().HaveCount(20);
		results.Select(x => x.PrimaryContactLastName)
			.Should().BeInAscendingOrder();
		results.Should().NotContain(x => x.PrimaryContactLastName == "Family24");
	}

	private static FamilyProfile CreateProfile(string firstName, string lastName, string phone, string scanToken)
	{
		var profile = FamilyProfile.Create(Guid.NewGuid(), firstName, lastName, phone, null, null, DateTime.UtcNow);
		profile.WaiverStatus = WaiverStatus.None;
		profile.ScanToken = scanToken;
		return profile;
	}

	private static async Task<TestFixture> CreateFixtureAsync()
	{
		var databasePath = TestDatabasePaths.Create();
		var dbContextFactory = new TestDbContextFactory(databasePath);
		var initializer = new AppDbContextInitializer(dbContextFactory, NullLogger<AppDbContextInitializer>.Instance);
		await initializer.InitializeAsync();

		return new TestFixture(dbContextFactory, new FamilyProfileRepository(dbContextFactory));
	}

	private sealed class TestFixture : IAsyncDisposable
	{
		public TestFixture(TestDbContextFactory dbContextFactory, FamilyProfileRepository repository)
		{
			DbContextFactory = dbContextFactory;
			Repository = repository;
		}

		public TestDbContextFactory DbContextFactory { get; }

		public FamilyProfileRepository Repository { get; }

		public async ValueTask DisposeAsync()
		{
			await DbContextFactory.DisposeAsync();
		}
	}
}
