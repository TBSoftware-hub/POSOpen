using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.UseCases.StaffManagement;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Unit.StaffManagement;

public sealed class UpdateStaffAccountUseCaseTests
{
	[Fact]
	public async Task ExecuteAsync_returns_not_found_when_staff_does_not_exist()
	{
		var repository = new InMemoryStaffAccountRepository();
		var operationLogs = new RecordingOperationLogRepository();
		var useCase = new UpdateStaffAccountUseCase(repository, operationLogs, NullLogger<UpdateStaffAccountUseCase>.Instance);

		var result = await useCase.ExecuteAsync(new UpdateStaffAccountCommand(
			Guid.NewGuid(),
			"Alice",
			"Smith",
			"alice@example.com",
			StaffRole.Manager,
			new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow),
			Guid.NewGuid()));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be("STAFF_NOT_FOUND");
	}

	[Fact]
	public async Task ExecuteAsync_returns_email_conflict_when_email_is_owned_by_other_account()
	{
		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow);
		var repository = new InMemoryStaffAccountRepository();
		var operationLogs = new RecordingOperationLogRepository();
		var useCase = new UpdateStaffAccountUseCase(repository, operationLogs, NullLogger<UpdateStaffAccountUseCase>.Instance);

		var existing = StaffAccount.Create(Guid.NewGuid(), "Alice", "One", "alice@example.com", "hash", "salt", StaffRole.Manager, StaffAccountStatus.Active, context.OccurredUtc, context.OccurredUtc, null, null);
		var other = StaffAccount.Create(Guid.NewGuid(), "Bob", "Two", "bob@example.com", "hash", "salt", StaffRole.Cashier, StaffAccountStatus.Active, context.OccurredUtc, context.OccurredUtc, null, null);
		await repository.AddAsync(existing);
		await repository.AddAsync(other);

		var result = await useCase.ExecuteAsync(new UpdateStaffAccountCommand(
			other.Id,
			other.FirstName,
			other.LastName,
			existing.Email,
			other.Role,
			context,
			Guid.NewGuid()));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be("STAFF_EMAIL_CONFLICT");
	}

	[Fact]
	public async Task ExecuteAsync_trims_names_and_normalizes_email()
	{
		var now = new DateTime(2026, 3, 29, 15, 0, 0, DateTimeKind.Utc);
		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, now);
		var repository = new InMemoryStaffAccountRepository();
		var operationLogs = new RecordingOperationLogRepository();
		var useCase = new UpdateStaffAccountUseCase(repository, operationLogs, NullLogger<UpdateStaffAccountUseCase>.Instance);

		var account = StaffAccount.Create(Guid.NewGuid(), "Old", "Name", "old@example.com", "hash", "salt", StaffRole.Cashier, StaffAccountStatus.Active, now.AddMinutes(-5), now.AddMinutes(-5), null, null);
		await repository.AddAsync(account);

		var result = await useCase.ExecuteAsync(new UpdateStaffAccountCommand(
			account.Id,
			"  Alice  ",
			"  Smith  ",
			"  ALICE@Example.COM  ",
			StaffRole.Admin,
			context,
			Guid.NewGuid()));

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.FirstName.Should().Be("Alice");
		result.Payload.LastName.Should().Be("Smith");
		result.Payload.Email.Should().Be("alice@example.com");
		result.Payload.Role.Should().Be(StaffRole.Cashier);
		account.UpdatedAtUtc.Should().Be(now);
		operationLogs.Entries.Should().ContainSingle(entry => entry.EventType == "StaffAccountUpdated");
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

		public Task<IReadOnlyList<StaffAccount>> ListActiveAsync(CancellationToken ct = default)
		{
			return Task.FromResult<IReadOnlyList<StaffAccount>>(_accounts.Where(account => account.Status == StaffAccountStatus.Active).ToList());
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