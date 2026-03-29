using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Security;
using POSOpen.Application.UseCases.StaffManagement;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Infrastructure.Security;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Unit.StaffManagement;

public sealed class AssignStaffRoleUseCaseTests
{
	[Fact]
	public async Task ExecuteAsync_assigns_role_for_owner_and_increments_session_version()
	{
		var now = new DateTime(2026, 3, 29, 17, 0, 0, DateTimeKind.Utc);
		var repository = new InMemoryStaffAccountRepository();
		var operationLogs = new RecordingOperationLogRepository();
		var sessionService = new TestCurrentSessionService(new CurrentSession(Guid.NewGuid(), StaffRole.Owner, 5, 5));
		var useCase = new AssignStaffRoleUseCase(
			repository,
			operationLogs,
			new AuthorizationPolicyService(),
			sessionService,
			NullLogger<AssignStaffRoleUseCase>.Instance);

		var account = StaffAccount.Create(Guid.NewGuid(), "Alex", "Lane", "alex@example.com", "hash", "salt", StaffRole.Cashier, StaffAccountStatus.Active, now, now, null, null);
		await repository.AddAsync(account);

		var result = await useCase.ExecuteAsync(new AssignStaffRoleCommand(
			account.Id,
			StaffRole.Manager,
			new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, now.AddMinutes(1))));

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.Role.Should().Be(StaffRole.Manager);
		sessionService.SessionVersion.Should().Be(6);
		operationLogs.Entries.Should().ContainSingle(entry => entry.EventType == "StaffRoleAssigned");
	}

	[Fact]
	public async Task ExecuteAsync_denies_cashier_actor_with_user_safe_message()
	{
		var repository = new InMemoryStaffAccountRepository();
		var operationLogs = new RecordingOperationLogRepository();
		var sessionService = new TestCurrentSessionService(new CurrentSession(Guid.NewGuid(), StaffRole.Cashier, 1, 1));
		var useCase = new AssignStaffRoleUseCase(
			repository,
			operationLogs,
			new AuthorizationPolicyService(),
			sessionService,
			NullLogger<AssignStaffRoleUseCase>.Instance);

		var result = await useCase.ExecuteAsync(new AssignStaffRoleCommand(
			Guid.NewGuid(),
			StaffRole.Manager,
			new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow)));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be("AUTH_FORBIDDEN");
		result.UserMessage.Should().Be("You do not have access to this action.");
	}

	[Fact]
	public async Task ExecuteAsync_returns_not_found_when_target_staff_missing()
	{
		var useCase = new AssignStaffRoleUseCase(
			new InMemoryStaffAccountRepository(),
			new RecordingOperationLogRepository(),
			new AuthorizationPolicyService(),
			new TestCurrentSessionService(new CurrentSession(Guid.NewGuid(), StaffRole.Admin, 1, 1)),
			NullLogger<AssignStaffRoleUseCase>.Instance);

		var result = await useCase.ExecuteAsync(new AssignStaffRoleCommand(
			Guid.NewGuid(),
			StaffRole.Manager,
			new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow)));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be("STAFF_NOT_FOUND");
	}

	[Fact]
	public async Task ExecuteAsync_returns_no_change_when_role_already_assigned()
	{
		var now = DateTime.UtcNow;
		var repository = new InMemoryStaffAccountRepository();
		var operationLogs = new RecordingOperationLogRepository();
		var sessionService = new TestCurrentSessionService(new CurrentSession(Guid.NewGuid(), StaffRole.Admin, 3, 3));
		var useCase = new AssignStaffRoleUseCase(
			repository,
			operationLogs,
			new AuthorizationPolicyService(),
			sessionService,
			NullLogger<AssignStaffRoleUseCase>.Instance);

		var account = StaffAccount.Create(Guid.NewGuid(), "Alex", "Lane", "alex@example.com", "hash", "salt", StaffRole.Manager, StaffAccountStatus.Active, now, now, null, null);
		await repository.AddAsync(account);

		var result = await useCase.ExecuteAsync(new AssignStaffRoleCommand(
			account.Id,
			StaffRole.Manager,
			new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, now.AddMinutes(1))));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be("STAFF_ROLE_NO_CHANGE");
		operationLogs.Entries.Should().BeEmpty();
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

		public Task<IReadOnlyList<OperationLogEntry>> ListByEventTypesAsync(IReadOnlyList<string> eventTypes, CancellationToken cancellationToken = default)
		{
			return Task.FromResult<IReadOnlyList<OperationLogEntry>>(
				Entries.Where(e => eventTypes.Contains(e.EventType)).ToList());
		}
	}

	private sealed class TestCurrentSessionService : ICurrentSessionService
	{
		private readonly Guid _staffId;
		private readonly StaffRole _role;

		public TestCurrentSessionService(CurrentSession session)
		{
			_staffId = session.StaffId;
			_role = session.Role;
			SessionVersion = session.SessionVersion;
			PermissionSnapshotVersion = session.PermissionSnapshotVersion;
		}

		public long SessionVersion { get; private set; }

		public long PermissionSnapshotVersion { get; private set; }

		public CurrentSession? GetCurrent()
		{
			return new CurrentSession(_staffId, _role, SessionVersion, PermissionSnapshotVersion);
		}

		public void RefreshPermissionSnapshot()
		{
			PermissionSnapshotVersion = SessionVersion;
		}

		public long IncrementSessionVersion()
		{
			SessionVersion += 1;
			return SessionVersion;
		}
	}
}
