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

public sealed class ProfileAdmissionUseCaseIntegrationTests
{
	[Fact]
	public async Task SubmitAsync_new_profile_persists_and_returns_family_id()
	{
		await using var fixture = await CreateFixtureAsync();
		var useCase = fixture.CreateUseCase();

		var result = await useCase.SubmitAsync(new SubmitProfileAdmissionCommand(null, "Ava", "Stone", "5551000", "a@b.com"));

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();

		var persisted = await fixture.Repository.GetByIdAsync(result.Payload!.FamilyId);
		persisted.Should().NotBeNull();
		persisted!.PrimaryContactFirstName.Should().Be("Ava");
		persisted.PrimaryContactLastName.Should().Be("Stone");
	}

	[Fact]
	public async Task SubmitAsync_incomplete_profile_updates_existing_record()
	{
		await using var fixture = await CreateFixtureAsync();
		var existing = FamilyProfile.Create(Guid.NewGuid(), "Ava", "", "", null, null, DateTime.UtcNow);
		await fixture.Repository.AddAsync(existing);

		var useCase = fixture.CreateUseCase();
		var result = await useCase.SubmitAsync(new SubmitProfileAdmissionCommand(existing.Id, "Ava", "Stone", "5551000", null));

		result.IsSuccess.Should().BeTrue();
		var updated = await fixture.Repository.GetByIdAsync(existing.Id);
		updated.Should().NotBeNull();
		updated!.PrimaryContactLastName.Should().Be("Stone");
		updated.Phone.Should().Be("5551000");
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

		public ProfileAdmissionUseCase CreateUseCase()
		{
			return new ProfileAdmissionUseCase(
				Repository,
				new AlwaysAuthorizedCurrentSessionService(),
				new AlwaysAuthorizedPolicyService(),
				NullLogger<ProfileAdmissionUseCase>.Instance);
		}

		public async ValueTask DisposeAsync()
		{
			await DbContextFactory.DisposeAsync();
		}
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
}
