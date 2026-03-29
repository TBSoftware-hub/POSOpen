using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.StaffManagement;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Unit.StaffManagement;

public sealed class CreateStaffAccountUseCaseTests
{
	[Fact]
	public async Task ExecuteAsync_with_valid_input_creates_staff_account_and_returns_success()
	{
		var repository = new InMemoryStaffAccountRepository();
		var operationLogs = new RecordingOperationLogRepository();
		var useCase = new CreateStaffAccountUseCase(repository, operationLogs, new TestPasswordHasher(), NullLogger<CreateStaffAccountUseCase>.Instance);

		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow);
		var command = new CreateStaffAccountCommand("Alice", "Smith", "alice@example.com", "P@ssword1", StaffRole.Admin, context, Guid.NewGuid());

		var result = await useCase.ExecuteAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.Email.Should().Be("alice@example.com");
		result.Payload.Role.Should().Be(StaffRole.Admin);
		result.Payload.Status.Should().Be(StaffAccountStatus.Active);
		operationLogs.Entries.Should().ContainSingle(entry => entry.EventType == "StaffAccountCreated");
	}

	[Fact]
	public async Task ExecuteAsync_with_duplicate_email_returns_conflict_failure()
	{
		var repository = new InMemoryStaffAccountRepository();
		var operationLogs = new RecordingOperationLogRepository();
		var useCase = new CreateStaffAccountUseCase(repository, operationLogs, new TestPasswordHasher(), NullLogger<CreateStaffAccountUseCase>.Instance);
		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow);

		await useCase.ExecuteAsync(new CreateStaffAccountCommand("Alice", "One", "alice@example.com", "P@ssword1", StaffRole.Cashier, context, null));
		var duplicate = await useCase.ExecuteAsync(new CreateStaffAccountCommand("Alice", "Two", "alice@example.com", "P@ssword1", StaffRole.Cashier, context, null));

		duplicate.IsSuccess.Should().BeFalse();
		duplicate.ErrorCode.Should().Be("STAFF_EMAIL_CONFLICT");
	}

	[Fact]
	public async Task ExecuteAsync_with_missing_first_name_returns_validation_failure()
	{
		var useCase = new CreateStaffAccountUseCase(
			new InMemoryStaffAccountRepository(),
			new RecordingOperationLogRepository(),
			new TestPasswordHasher(),
			NullLogger<CreateStaffAccountUseCase>.Instance);

		var result = await useCase.ExecuteAsync(new CreateStaffAccountCommand(
			string.Empty,
			"Smith",
			"alice@example.com",
			"P@ssword1",
			StaffRole.Manager,
			new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow),
			null));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be("STAFF_VALIDATION_FIRST_NAME_REQUIRED");
	}

	[Fact]
	public async Task ExecuteAsync_with_short_password_returns_failure()
	{
		var useCase = new CreateStaffAccountUseCase(
			new InMemoryStaffAccountRepository(),
			new RecordingOperationLogRepository(),
			new TestPasswordHasher(),
			NullLogger<CreateStaffAccountUseCase>.Instance);

		var result = await useCase.ExecuteAsync(new CreateStaffAccountCommand(
			"Alice",
			"Smith",
			"alice@example.com",
			"1234567",
			StaffRole.Manager,
			new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow),
			null));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be("STAFF_VALIDATION_PASSWORD_TOO_SHORT");
	}

	private sealed class InMemoryStaffAccountRepository : IStaffAccountRepository
	{
		private readonly List<StaffAccount> _accounts = new();

		public Task AddAsync(StaffAccount account, CancellationToken ct = default)
		{
			_accounts.Add(account);
			return Task.CompletedTask;
		}

		public Task<StaffAccount?> GetByIdAsync(Guid id, CancellationToken ct = default)
		{
			return Task.FromResult(_accounts.SingleOrDefault(account => account.Id == id));
		}

		public Task<StaffAccount?> GetByEmailAsync(string email, CancellationToken ct = default)
		{
			var normalized = email.Trim().ToLowerInvariant();
			return Task.FromResult(_accounts.SingleOrDefault(account => account.Email == normalized));
		}

		public Task<StaffAccount?> GetByNormalizedEmailForAuthenticationAsync(string email, CancellationToken ct = default)
		{
			var normalized = email.Trim().ToLowerInvariant();
			return Task.FromResult(_accounts.SingleOrDefault(account => account.Email == normalized));
		}

		public Task<IReadOnlyList<StaffAccount>> ListActiveAsync(CancellationToken ct = default)
		{
			return Task.FromResult<IReadOnlyList<StaffAccount>>(_accounts.Where(account => account.Status == StaffAccountStatus.Active).ToList());
		}

		public Task<StaffAccount?> RecordFailedSignInAttemptAsync(Guid staffAccountId, DateTime occurredUtc, int lockoutThreshold, TimeSpan lockoutDuration, CancellationToken ct = default)
		{
			var account = _accounts.SingleOrDefault(x => x.Id == staffAccountId);
			if (account is null)
			{
				return Task.FromResult<StaffAccount?>(null);
			}

			account.FailedLoginAttempts += 1;
			if (account.FailedLoginAttempts >= lockoutThreshold)
			{
				account.LockedUntilUtc = occurredUtc.Add(lockoutDuration);
			}

			account.UpdatedAtUtc = occurredUtc;
			return Task.FromResult<StaffAccount?>(account);
		}

		public Task<StaffAccount?> RecordSuccessfulSignInAsync(Guid staffAccountId, DateTime occurredUtc, CancellationToken ct = default)
		{
			var account = _accounts.SingleOrDefault(x => x.Id == staffAccountId);
			if (account is null)
			{
				return Task.FromResult<StaffAccount?>(null);
			}

			account.FailedLoginAttempts = 0;
			account.LockedUntilUtc = null;
			account.UpdatedAtUtc = occurredUtc;
			return Task.FromResult<StaffAccount?>(account);
		}

		public Task UpdateAsync(StaffAccount account, CancellationToken ct = default)
		{
			return Task.CompletedTask;
		}
	}

	private sealed class RecordingOperationLogRepository : IOperationLogRepository
	{
		public List<OperationLogEntry> Entries { get; } = new();

		public Task<OperationLogEntry> AppendAsync<TPayload>(string eventType, string aggregateId, TPayload payload, OperationContext operationContext, int version = 1, CancellationToken cancellationToken = default)
		{
			var entry = new OperationLogEntry
			{
				Id = Guid.NewGuid(),
				EventId = Guid.NewGuid().ToString("N"),
				EventType = eventType,
				AggregateId = aggregateId,
				OperationId = operationContext.OperationId,
				CorrelationId = operationContext.CorrelationId,
				CausationId = operationContext.CausationId,
				Version = version,
				PayloadJson = "{}",
				OccurredUtc = operationContext.OccurredUtc,
				RecordedUtc = operationContext.OccurredUtc
			};
			Entries.Add(entry);
			return Task.FromResult(entry);
		}

		public Task<IReadOnlyList<OperationLogEntry>> ListAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult<IReadOnlyList<OperationLogEntry>>(Entries);
		}
	}

	private sealed class TestPasswordHasher : IPasswordHasher
	{
		public (string Hash, string Salt) Hash(string plaintext)
		{
			return ($"hash-{plaintext}", "salt-test");
		}

		public bool Verify(string plaintext, string storedHash, string storedSalt)
		{
			return storedHash == $"hash-{plaintext}" && storedSalt == "salt-test";
		}
	}
}
