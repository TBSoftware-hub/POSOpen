using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.UseCases.StaffManagement;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Unit.StaffManagement;

public sealed class DeactivateStaffAccountUseCaseTests
{
	[Fact]
	public async Task ExecuteAsync_deactivates_active_account_and_emits_log()
	{
		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, new DateTime(2026, 3, 29, 12, 0, 0, DateTimeKind.Utc));
		var repository = new InMemoryStaffAccountRepository();
		var operationLogs = new RecordingOperationLogRepository();
		var account = StaffAccount.Create(Guid.NewGuid(), "A", "B", "active@example.com", "hash", "salt", StaffRole.Admin, StaffAccountStatus.Active, context.OccurredUtc, context.OccurredUtc, null, null);
		await repository.AddAsync(account);
		var useCase = new DeactivateStaffAccountUseCase(repository, operationLogs, NullLogger<DeactivateStaffAccountUseCase>.Instance);

		var result = await useCase.ExecuteAsync(new DeactivateStaffAccountCommand(account.Id, context, Guid.NewGuid()));

		result.IsSuccess.Should().BeTrue();
		account.Status.Should().Be(StaffAccountStatus.Inactive);
		account.UpdatedAtUtc.Should().Be(context.OccurredUtc);
		operationLogs.Entries.Should().ContainSingle(entry => entry.EventType == "StaffAccountDeactivated");
	}

	[Fact]
	public async Task ExecuteAsync_returns_failure_when_already_inactive()
	{
		var repository = new InMemoryStaffAccountRepository();
		var account = StaffAccount.Create(Guid.NewGuid(), "A", "B", "inactive@example.com", "hash", "salt", StaffRole.Admin, StaffAccountStatus.Inactive, DateTime.UtcNow, DateTime.UtcNow, null, null);
		await repository.AddAsync(account);
		var useCase = new DeactivateStaffAccountUseCase(repository, new RecordingOperationLogRepository(), NullLogger<DeactivateStaffAccountUseCase>.Instance);

		var result = await useCase.ExecuteAsync(new DeactivateStaffAccountCommand(account.Id, new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow), Guid.NewGuid()));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be("STAFF_ALREADY_INACTIVE");
	}

	[Fact]
	public async Task ExecuteAsync_returns_failure_when_staff_not_found()
	{
		var useCase = new DeactivateStaffAccountUseCase(new InMemoryStaffAccountRepository(), new RecordingOperationLogRepository(), NullLogger<DeactivateStaffAccountUseCase>.Instance);

		var result = await useCase.ExecuteAsync(new DeactivateStaffAccountCommand(Guid.NewGuid(), new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow), Guid.NewGuid()));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be("STAFF_NOT_FOUND");
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
}
