using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Authentication;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Infrastructure.Security;
using POSOpen.Infrastructure.Services;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Unit.Security;

public sealed class AuthenticateStaffUseCaseTests
{
	[Fact]
	public async Task ExecuteAsync_with_valid_credentials_creates_session_and_returns_role_route()
	{
		var now = new DateTime(2026, 3, 29, 20, 0, 0, DateTimeKind.Utc);
		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, now);
		var (repository, account) = CreateRepositoryWithAccount("staff@example.com", "Passphrase9", StaffRole.Admin, StaffAccountStatus.Active, now);
		var logs = new RecordingOperationLogRepository();
		var appState = new AppStateService();
		var useCase = new AuthenticateStaffUseCase(
			repository,
			logs,
			new Pbkdf2PasswordHasher(),
			appState,
			NullLogger<AuthenticateStaffUseCase>.Instance);

		var result = await useCase.ExecuteAsync(new AuthenticateStaffCommand(" STAFF@example.com ", "Passphrase9", context));

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.StaffId.Should().Be(account.Id);
		result.Payload.Role.Should().Be(StaffRole.Admin);
		result.Payload.NextRoute.Should().Be("staff/list");
		appState.IsAuthenticated.Should().BeTrue();
		appState.CurrentStaffId.Should().Be(account.Id);
		appState.CurrentStaffRole.Should().Be(StaffRole.Admin);
		appState.SessionVersion.Should().Be(1);
		logs.Entries.Should().ContainSingle(entry => entry.EventType == "StaffAuthenticationSucceeded");
		logs.Entries.Should().NotContain(entry => entry.EventType == "StaffAuthenticationDenied");
	}

	[Fact]
	public async Task ExecuteAsync_with_invalid_password_denies_and_does_not_create_session()
	{
		var now = new DateTime(2026, 3, 29, 20, 5, 0, DateTimeKind.Utc);
		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, now);
		var (repository, _) = CreateRepositoryWithAccount("staff@example.com", "Passphrase9", StaffRole.Cashier, StaffAccountStatus.Active, now);
		var logs = new RecordingOperationLogRepository();
		var appState = new AppStateService();
		var useCase = new AuthenticateStaffUseCase(
			repository,
			logs,
			new Pbkdf2PasswordHasher(),
			appState,
			NullLogger<AuthenticateStaffUseCase>.Instance);

		var result = await useCase.ExecuteAsync(new AuthenticateStaffCommand("staff@example.com", "WrongPass", context));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(AuthenticationConstants.ErrorInvalidCredentials);
		result.UserMessage.Should().Be(AuthenticationConstants.SafeSignInFailureMessage);
		appState.IsAuthenticated.Should().BeFalse();
		logs.Entries.Should().ContainSingle(entry => entry.EventType == "StaffAuthenticationDenied");
	}

	[Fact]
	public async Task ExecuteAsync_with_unknown_email_denies_with_non_revealing_message()
	{
		var now = new DateTime(2026, 3, 29, 20, 10, 0, DateTimeKind.Utc);
		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, now);
		var repository = new InMemoryStaffAccountRepository();
		var logs = new RecordingOperationLogRepository();
		var appState = new AppStateService();
		var useCase = new AuthenticateStaffUseCase(
			repository,
			logs,
			new Pbkdf2PasswordHasher(),
			appState,
			NullLogger<AuthenticateStaffUseCase>.Instance);

		var result = await useCase.ExecuteAsync(new AuthenticateStaffCommand("missing@example.com", "Passphrase9", context));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(AuthenticationConstants.ErrorInvalidCredentials);
		result.UserMessage.Should().Be(AuthenticationConstants.SafeSignInFailureMessage);
		appState.IsAuthenticated.Should().BeFalse();
	}

	[Fact]
	public async Task ExecuteAsync_with_inactive_account_denies_without_session()
	{
		var now = new DateTime(2026, 3, 29, 20, 15, 0, DateTimeKind.Utc);
		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, now);
		var (repository, _) = CreateRepositoryWithAccount("staff@example.com", "Passphrase9", StaffRole.Manager, StaffAccountStatus.Inactive, now);
		var logs = new RecordingOperationLogRepository();
		var appState = new AppStateService();
		var useCase = new AuthenticateStaffUseCase(
			repository,
			logs,
			new Pbkdf2PasswordHasher(),
			appState,
			NullLogger<AuthenticateStaffUseCase>.Instance);

		var result = await useCase.ExecuteAsync(new AuthenticateStaffCommand("staff@example.com", "Passphrase9", context));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(AuthenticationConstants.ErrorAccountInactive);
		result.UserMessage.Should().Be(AuthenticationConstants.SafeSignInFailureMessage);
		appState.IsAuthenticated.Should().BeFalse();
		logs.Entries.Should().ContainSingle(entry => entry.EventType == "StaffAuthenticationDenied");
	}

	[Fact]
	public async Task ExecuteAsync_locks_account_after_fifth_failed_attempt()
	{
		var now = new DateTime(2026, 3, 29, 20, 20, 0, DateTimeKind.Utc);
		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, now);
		var (repository, account) = CreateRepositoryWithAccount("staff@example.com", "Passphrase9", StaffRole.Cashier, StaffAccountStatus.Active, now);
		account.FailedLoginAttempts = 4;
		var logs = new RecordingOperationLogRepository();
		var useCase = new AuthenticateStaffUseCase(
			repository,
			logs,
			new Pbkdf2PasswordHasher(),
			new AppStateService(),
			NullLogger<AuthenticateStaffUseCase>.Instance);

		var result = await useCase.ExecuteAsync(new AuthenticateStaffCommand("staff@example.com", "WrongPass", context));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(AuthenticationConstants.ErrorAccountLocked);
		account.FailedLoginAttempts.Should().Be(5);
		account.LockedUntilUtc.Should().Be(now.AddMinutes(15));
	}

	private static (InMemoryStaffAccountRepository Repository, StaffAccount Account) CreateRepositoryWithAccount(
		string email,
		string password,
		StaffRole role,
		StaffAccountStatus status,
		DateTime now)
	{
		var hasher = new Pbkdf2PasswordHasher();
		var repository = new InMemoryStaffAccountRepository();
		var (hash, salt) = hasher.Hash(password);
		var account = StaffAccount.Create(
			Guid.NewGuid(),
			"Alex",
			"Lane",
			email.Trim().ToLowerInvariant(),
			hash,
			salt,
			role,
			status,
			now,
			now,
			null,
			null);

		repository.AddAsync(account).GetAwaiter().GetResult();
		return (repository, account);
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

		public Task<OperationLogEntry> AppendAsync<TPayload>(
			string eventType,
			string aggregateId,
			TPayload payload,
			OperationContext operationContext,
			int version = 1,
			CancellationToken cancellationToken = default)
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
