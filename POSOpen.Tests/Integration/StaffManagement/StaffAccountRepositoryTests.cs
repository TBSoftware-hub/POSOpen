using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using POSOpen.Application.UseCases.StaffManagement;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Infrastructure.Persistence;
using POSOpen.Infrastructure.Persistence.Repositories;
using POSOpen.Infrastructure.Security;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Integration.StaffManagement;

public sealed class StaffAccountRepositoryTests
{
	[Fact]
	public async Task Repository_supports_add_get_update_and_active_listing()
	{
		var databasePath = TestDatabasePaths.Create();
		await using var dbContextFactory = new TestDbContextFactory(databasePath);
		var initializer = new AppDbContextInitializer(dbContextFactory, NullLogger<AppDbContextInitializer>.Instance);
		await initializer.InitializeAsync();

		var repository = new StaffAccountRepository(dbContextFactory);
		var now = new DateTime(2026, 3, 29, 15, 0, 0, DateTimeKind.Utc);
		var active = StaffAccount.Create(Guid.NewGuid(), "Alice", "Jones", "alice@example.com", "hash", "salt", StaffRole.Admin, StaffAccountStatus.Active, now, now, null, null);
		var inactive = StaffAccount.Create(Guid.NewGuid(), "Bob", "Jones", "bob@example.com", "hash", "salt", StaffRole.Cashier, StaffAccountStatus.Inactive, now, now, null, null);

		await repository.AddAsync(active);
		await repository.AddAsync(inactive);

		var byId = await repository.GetByIdAsync(active.Id);
		var byEmail = await repository.GetByEmailAsync("ALICE@EXAMPLE.COM");
		var activeList = await repository.ListActiveAsync();

		byId.Should().NotBeNull();
		byId!.FirstName.Should().Be("Alice");
		byEmail.Should().NotBeNull();
		byEmail!.Id.Should().Be(active.Id);
		activeList.Should().ContainSingle(account => account.Id == active.Id);

		active.FirstName = "Alicia";
		active.Status = StaffAccountStatus.Inactive;
		active.UpdatedAtUtc = now.AddMinutes(5);
		await repository.UpdateAsync(active);

		var updated = await repository.GetByIdAsync(active.Id);
		updated.Should().NotBeNull();
		updated!.FirstName.Should().Be("Alicia");
		updated.CreatedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
		updated.UpdatedAtUtc.Kind.Should().Be(DateTimeKind.Utc);

		var activeAfterDeactivate = await repository.ListActiveAsync();
		activeAfterDeactivate.Should().BeEmpty();
	}

	[Fact]
	public async Task Repository_enforces_unique_email_constraint()
	{
		var databasePath = TestDatabasePaths.Create();
		await using var dbContextFactory = new TestDbContextFactory(databasePath);
		var initializer = new AppDbContextInitializer(dbContextFactory, NullLogger<AppDbContextInitializer>.Instance);
		await initializer.InitializeAsync();
		var repository = new StaffAccountRepository(dbContextFactory);
		var now = DateTime.UtcNow;

		await repository.AddAsync(StaffAccount.Create(Guid.NewGuid(), "Alice", "One", "dupe@example.com", "hash", "salt", StaffRole.Admin, StaffAccountStatus.Active, now, now, null, null));
		var duplicate = StaffAccount.Create(Guid.NewGuid(), "Alice", "Two", "dupe@example.com", "hash", "salt", StaffRole.Cashier, StaffAccountStatus.Active, now, now, null, null);

		Func<Task> action = async () => await repository.AddAsync(duplicate);
		await action.Should().ThrowAsync<DbUpdateException>();
	}

	[Fact]
	public async Task Repository_applies_auth_failed_attempt_lockout_and_success_reset_updates()
	{
		var databasePath = TestDatabasePaths.Create();
		await using var dbContextFactory = new TestDbContextFactory(databasePath);
		var initializer = new AppDbContextInitializer(dbContextFactory, NullLogger<AppDbContextInitializer>.Instance);
		await initializer.InitializeAsync();
		var repository = new StaffAccountRepository(dbContextFactory);
		var now = new DateTime(2026, 3, 29, 18, 0, 0, DateTimeKind.Utc);

		var account = StaffAccount.Create(
			Guid.NewGuid(),
			"Alex",
			"Taylor",
			"auth@example.com",
			"hash",
			"salt",
			StaffRole.Cashier,
			StaffAccountStatus.Active,
			now,
			now,
			null,
			null);

		await repository.AddAsync(account);

		for (var attempt = 1; attempt <= 5; attempt++)
		{
			await repository.RecordFailedSignInAttemptAsync(
				account.Id,
				now.AddMinutes(attempt),
				lockoutThreshold: 5,
				lockoutDuration: TimeSpan.FromMinutes(15));
		}

		var locked = await repository.GetByNormalizedEmailForAuthenticationAsync(" AUTH@EXAMPLE.COM ");
		locked.Should().NotBeNull();
		locked!.FailedLoginAttempts.Should().Be(5);
		locked.LockedUntilUtc.Should().Be(now.AddMinutes(20));

		await repository.RecordSuccessfulSignInAsync(account.Id, now.AddMinutes(30));

		var reset = await repository.GetByIdAsync(account.Id);
		reset.Should().NotBeNull();
		reset!.FailedLoginAttempts.Should().Be(0);
		reset.LockedUntilUtc.Should().BeNull();
	}

	[Fact]
	public async Task Create_use_case_writes_operation_log_entry_to_repository()
	{
		var databasePath = TestDatabasePaths.Create();
		await using var dbContextFactory = new TestDbContextFactory(databasePath);
		var initializer = new AppDbContextInitializer(dbContextFactory, NullLogger<AppDbContextInitializer>.Instance);
		await initializer.InitializeAsync();

		var staffRepository = new StaffAccountRepository(dbContextFactory);
		var logRepository = new OperationLogRepository(dbContextFactory, new TestUtcClock(new DateTime(2026, 3, 29, 16, 0, 0, DateTimeKind.Utc)));
		var hasher = new Pbkdf2PasswordHasher();
		var useCase = new CreateStaffAccountUseCase(staffRepository, logRepository, hasher, NullLogger<CreateStaffAccountUseCase>.Instance);
		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, new DateTime(2026, 3, 29, 16, 0, 0, DateTimeKind.Utc));

		var result = await useCase.ExecuteAsync(new CreateStaffAccountCommand(
			"Casey",
			"Taylor",
			"casey@example.com",
			"Passphrase9",
			StaffRole.Manager,
			context,
			null));

		result.IsSuccess.Should().BeTrue();
		var logs = await logRepository.ListAsync();
		logs.Should().Contain(entry =>
			entry.EventType == "StaffAccountCreated" &&
			entry.AggregateId == result.Payload!.Id.ToString());

		var updateUseCase = new UpdateStaffAccountUseCase(staffRepository, logRepository, NullLogger<UpdateStaffAccountUseCase>.Instance);
		var deactivateUseCase = new DeactivateStaffAccountUseCase(staffRepository, logRepository, NullLogger<DeactivateStaffAccountUseCase>.Instance);

		var updateContext = new OperationContext(Guid.NewGuid(), context.CorrelationId, context.OperationId, new DateTime(2026, 3, 29, 16, 5, 0, DateTimeKind.Utc));
		var updateResult = await updateUseCase.ExecuteAsync(new UpdateStaffAccountCommand(
			result.Payload!.Id,
			"Casey",
			"Taylor-Updated",
			"casey@example.com",
			StaffRole.Admin,
			updateContext,
			Guid.NewGuid()));
		updateResult.IsSuccess.Should().BeTrue();

		var deactivateContext = new OperationContext(Guid.NewGuid(), context.CorrelationId, updateContext.OperationId, new DateTime(2026, 3, 29, 16, 10, 0, DateTimeKind.Utc));
		var deactivateResult = await deactivateUseCase.ExecuteAsync(new DeactivateStaffAccountCommand(result.Payload.Id, deactivateContext, Guid.NewGuid()));
		deactivateResult.IsSuccess.Should().BeTrue();

		logs = await logRepository.ListAsync();
		logs.Should().Contain(entry => entry.EventType == "StaffAccountUpdated" && entry.AggregateId == result.Payload.Id.ToString());
		logs.Should().Contain(entry => entry.EventType == "StaffAccountDeactivated" && entry.AggregateId == result.Payload.Id.ToString());
	}
}
