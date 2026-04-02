using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Security;
using POSOpen.Application.UseCases.Inventory;
using POSOpen.Domain.Enums;
using POSOpen.Infrastructure.Persistence;
using POSOpen.Infrastructure.Persistence.Repositories;
using POSOpen.Infrastructure.Security;
using POSOpen.Infrastructure.Services;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Integration.Inventory;

public sealed class InventorySubstitutionPolicyManagementIntegrationTests
{
	[Fact]
	public async Task RepositoryCrud_RoundTripPersistsPolicyRows()
	{
		await using var fixture = await CreateFixtureAsync();
		var repository = new InventorySubstitutionPolicyRepository(fixture.DbContextFactory);
		var now = fixture.Clock.UtcNow;
		var policy = new POSOpen.Domain.Entities.InventorySubstitutionPolicy
		{
			Id = Guid.NewGuid(),
			SourceOptionId = "cake-custom",
			AllowedSubstituteOptionId = "cake-standard",
			AllowedRolesCsv = "Manager",
			IsActive = true,
			CreatedAtUtc = now,
			UpdatedAtUtc = now,
			CreatedByStaffId = Guid.NewGuid(),
			UpdatedByStaffId = Guid.NewGuid(),
			LastOperationId = Guid.NewGuid(),
		};

		await repository.AddAsync(policy);
		policy.IsActive = false;
		policy.UpdatedAtUtc = now.AddMinutes(1);
		await repository.UpdateAsync(policy);

		var rows = await repository.ListForManagementAsync();
		rows.Should().ContainSingle();
		rows[0].IsActive.Should().BeFalse();
	}

	[Fact]
	public async Task Provider_ExcludesInactiveRulesFromDownstreamSubstituteRead()
	{
		await using var fixture = await CreateFixtureAsync();
		var repository = new InventorySubstitutionPolicyRepository(fixture.DbContextFactory);
		var now = fixture.Clock.UtcNow;
		await repository.AddAsync(new POSOpen.Domain.Entities.InventorySubstitutionPolicy
		{
			Id = Guid.NewGuid(),
			SourceOptionId = "table-themed",
			AllowedSubstituteOptionId = "table-standard",
			AllowedRolesCsv = "Manager,Cashier",
			IsActive = true,
			CreatedAtUtc = now,
			UpdatedAtUtc = now,
			CreatedByStaffId = Guid.NewGuid(),
			UpdatedByStaffId = Guid.NewGuid(),
			LastOperationId = Guid.NewGuid(),
		});
		await repository.AddAsync(new POSOpen.Domain.Entities.InventorySubstitutionPolicy
		{
			Id = Guid.NewGuid(),
			SourceOptionId = "table-themed",
			AllowedSubstituteOptionId = "table-standard",
			AllowedRolesCsv = "Manager",
			IsActive = false,
			CreatedAtUtc = now,
			UpdatedAtUtc = now,
			CreatedByStaffId = Guid.NewGuid(),
			UpdatedByStaffId = Guid.NewGuid(),
			LastOperationId = Guid.NewGuid(),
		});

		var provider = new RepositoryInventorySubstitutionPolicyProvider(repository);
		var useCase = new GetAllowedSubstitutesUseCase(provider);

		var result = await useCase.ExecuteAsync(new GetAllowedSubstitutesQuery(
			Guid.NewGuid(),
			StaffRole.Cashier,
			["table-themed"]));

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload.Should().ContainSingle();
		result.Payload![0].AllowedSubstituteOptionId.Should().Be("table-standard");
	}

	[Fact]
	public async Task UseCaseCrud_AppendsAuditEntriesForEachMutationType()
	{
		await using var fixture = await CreateFixtureAsync();
		var repository = new InventorySubstitutionPolicyRepository(fixture.DbContextFactory);
		var operationLogRepository = new OperationLogRepository(fixture.DbContextFactory, fixture.Clock);
		var session = new TestCurrentSessionService(new CurrentSession(Guid.NewGuid(), StaffRole.Manager, 1, 1));
		var authorization = new AuthorizationPolicyService();

		var createUseCase = new CreateInventorySubstitutionPolicyUseCase(repository, session, authorization, operationLogRepository);
		var createResult = await createUseCase.ExecuteAsync(new CreateInventorySubstitutionPolicyCommand(
			"cake-custom",
			"cake-standard",
			[StaffRole.Manager],
			true,
			NewContext(fixture.Clock.UtcNow)));
		createResult.IsSuccess.Should().BeTrue();
		createResult.Payload.Should().NotBeNull();

		var updateUseCase = new UpdateInventorySubstitutionPolicyUseCase(repository, session, authorization, operationLogRepository);
		var updateResult = await updateUseCase.ExecuteAsync(new UpdateInventorySubstitutionPolicyCommand(
			createResult.Payload!.PolicyId,
			"cake-custom",
			"cake-standard",
			[StaffRole.Manager, StaffRole.Cashier],
			true,
			NewContext(fixture.Clock.UtcNow.AddMinutes(1))));
		updateResult.IsSuccess.Should().BeTrue();

		var deleteUseCase = new DeleteInventorySubstitutionPolicyUseCase(repository, session, authorization, operationLogRepository);
		var deleteResult = await deleteUseCase.ExecuteAsync(new DeleteInventorySubstitutionPolicyCommand(
			createResult.Payload.PolicyId,
			NewContext(fixture.Clock.UtcNow.AddMinutes(2))));
		deleteResult.IsSuccess.Should().BeTrue();

		await using var dbContext = await fixture.DbContextFactory.CreateDbContextAsync();
		var eventTypes = dbContext.OperationLogEntries
			.Select(x => x.EventType)
			.ToArray();
		eventTypes.Should().Contain(SecurityAuditEventTypes.InventorySubstitutionPolicyCreated);
		eventTypes.Should().Contain(SecurityAuditEventTypes.InventorySubstitutionPolicyUpdated);
		eventTypes.Should().Contain(SecurityAuditEventTypes.InventorySubstitutionPolicyDeleted);
	}

	[Fact]
	public async Task ValidationOrAuthorizationFailure_DoesNotPersistPolicyRows()
	{
		await using var fixture = await CreateFixtureAsync();
		var repository = new InventorySubstitutionPolicyRepository(fixture.DbContextFactory);
		var operationLogRepository = new OperationLogRepository(fixture.DbContextFactory, fixture.Clock);

		var unauthorizedCreate = new CreateInventorySubstitutionPolicyUseCase(
			repository,
			new TestCurrentSessionService(new CurrentSession(Guid.NewGuid(), StaffRole.Cashier, 1, 1)),
			new AuthorizationPolicyService(),
			operationLogRepository);

		var unauthorizedResult = await unauthorizedCreate.ExecuteAsync(new CreateInventorySubstitutionPolicyCommand(
			"cake-custom",
			"cake-standard",
			[StaffRole.Manager],
			true,
			NewContext(fixture.Clock.UtcNow)));
		unauthorizedResult.IsSuccess.Should().BeFalse();

		var managerCreate = new CreateInventorySubstitutionPolicyUseCase(
			repository,
			new TestCurrentSessionService(new CurrentSession(Guid.NewGuid(), StaffRole.Manager, 1, 1)),
			new AuthorizationPolicyService(),
			operationLogRepository);

		var invalidResult = await managerCreate.ExecuteAsync(new CreateInventorySubstitutionPolicyCommand(
			"unknown-option",
			"cake-standard",
			[StaffRole.Manager],
			true,
			NewContext(fixture.Clock.UtcNow.AddMinutes(1))));
		invalidResult.IsSuccess.Should().BeFalse();

		var rows = await repository.ListForManagementAsync();
		rows.Should().BeEmpty();
	}

	private static async Task<TestFixture> CreateFixtureAsync()
	{
		var databasePath = TestDatabasePaths.Create();
		var dbContextFactory = new TestDbContextFactory(databasePath);
		var initializer = new AppDbContextInitializer(dbContextFactory, NullLogger<AppDbContextInitializer>.Instance);
		await initializer.InitializeAsync();
		var clock = new TestUtcClock(new DateTime(2026, 4, 26, 9, 0, 0, DateTimeKind.Utc));
		return new TestFixture(dbContextFactory, clock);
	}

	private static OperationContext NewContext(DateTime occurredUtc) =>
		new(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.SpecifyKind(occurredUtc, DateTimeKind.Utc));

	private sealed class TestFixture : IAsyncDisposable
	{
		public TestFixture(TestDbContextFactory dbContextFactory, TestUtcClock clock)
		{
			DbContextFactory = dbContextFactory;
			Clock = clock;
		}

		public TestDbContextFactory DbContextFactory { get; }
		public TestUtcClock Clock { get; }

		public async ValueTask DisposeAsync()
		{
			await DbContextFactory.DisposeAsync();
		}
	}

	private sealed class TestCurrentSessionService : ICurrentSessionService
	{
		private readonly CurrentSession _session;

		public TestCurrentSessionService(CurrentSession session)
		{
			_session = session;
		}

		public CurrentSession? GetCurrent() => _session;

		public void RefreshPermissionSnapshot()
		{
		}

		public long IncrementSessionVersion()
		{
			return _session.SessionVersion;
		}
	}
}
