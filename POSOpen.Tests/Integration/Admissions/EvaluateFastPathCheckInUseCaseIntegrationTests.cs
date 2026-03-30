using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Security;
using POSOpen.Application.UseCases.Admissions;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Infrastructure.Persistence;
using POSOpen.Infrastructure.Persistence.Repositories;

namespace POSOpen.Tests.Integration.Admissions;

public sealed class EvaluateFastPathCheckInUseCaseIntegrationTests
{
	[Fact]
	public async Task ExecuteAsync_re_evaluation_reads_latest_waiver_status()
	{
		await using var fixture = await CreateFixtureAsync();
		var profile = CreateProfile("Jamie", "River", "0400-000-001", WaiverStatus.Pending);
		await fixture.Repository.AddAsync(profile);

		var useCase = new EvaluateFastPathCheckInUseCase(
			fixture.Repository,
			new AlwaysAuthorizedCurrentSessionService(),
			new AlwaysAuthorizedPolicyService(),
			NullLogger<EvaluateFastPathCheckInUseCase>.Instance);

		var first = await useCase.ExecuteAsync(new EvaluateFastPathCheckInQuery(profile.Id, IsRefreshRequested: false));
		first.IsSuccess.Should().BeTrue();
		first.Payload!.State.Should().Be(FastPathEligibilityState.RefreshRequired);

		var latest = await fixture.Repository.GetByIdAsync(profile.Id);
		latest.Should().NotBeNull();
		latest!.WaiverStatus = WaiverStatus.Valid;
		latest.WaiverCompletedAtUtc = DateTime.UtcNow;
		await fixture.Repository.UpdateAsync(latest);

		var refreshed = await useCase.ExecuteAsync(new EvaluateFastPathCheckInQuery(profile.Id, IsRefreshRequested: true));
		refreshed.IsSuccess.Should().BeTrue();
		refreshed.Payload!.State.Should().Be(FastPathEligibilityState.Allowed);
		refreshed.Payload.IsEligible.Should().BeTrue();
	}

	private static FamilyProfile CreateProfile(string firstName, string lastName, string phone, WaiverStatus status)
	{
		var profile = FamilyProfile.Create(Guid.NewGuid(), firstName, lastName, phone, null, null, DateTime.UtcNow);
		profile.WaiverStatus = status;
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

	private sealed class AlwaysAuthorizedCurrentSessionService : ICurrentSessionService
	{
		private readonly CurrentSession _session = new(Guid.NewGuid(), StaffRole.Cashier, 1, 1);
		private long _sessionVersion;

		public CurrentSession? GetCurrent()
		{
			return _session;
		}

		public void RefreshPermissionSnapshot()
		{
		}

		public long IncrementSessionVersion()
		{
			_sessionVersion++;
			return _sessionVersion;
		}
	}

	private sealed class AlwaysAuthorizedPolicyService : IAuthorizationPolicyService
	{
		public bool HasPermission(StaffRole role, string permission)
		{
			return permission == RolePermissions.AdmissionsLookup;
		}
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
