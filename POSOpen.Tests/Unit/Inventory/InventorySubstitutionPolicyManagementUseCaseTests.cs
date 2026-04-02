using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Security;
using POSOpen.Application.UseCases.Inventory;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Infrastructure.Security;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Unit.Inventory;

public sealed class InventorySubstitutionPolicyManagementUseCaseTests
{
	[Fact]
	public async Task ListUseCase_ReturnsDeterministicOrderingAndDisplayNames()
	{
		var repository = new InMemoryRepository(
		[
			BuildPolicy("table-themed", "table-standard", "Manager", true),
			BuildPolicy("banner-custom", "banner-standard", "Cashier,Manager", true)
		]);
		var sut = new GetInventorySubstitutionPoliciesUseCase(
			repository,
			new TestCurrentSessionService(new CurrentSession(Guid.NewGuid(), StaffRole.Manager, 1, 1)),
			new AuthorizationPolicyService());

		var result = await sut.ExecuteAsync(new GetInventorySubstitutionPoliciesQuery(NewContext()));

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.Select(x => x.SourceOptionId).Should().Equal("banner-custom", "table-themed");
		result.Payload[0].SourceDisplayName.Should().Be("Banner (Custom)");
	}

	[Fact]
	public async Task ListUseCase_DeniesUnauthorizedRole()
	{
		var sut = new GetInventorySubstitutionPoliciesUseCase(
			new InMemoryRepository([]),
			new TestCurrentSessionService(new CurrentSession(Guid.NewGuid(), StaffRole.Cashier, 1, 1)),
			new AuthorizationPolicyService());

		var result = await sut.ExecuteAsync(new GetInventorySubstitutionPoliciesQuery(NewContext()));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(InventorySubstitutionPolicyConstants.ErrorAuthForbidden);
	}

	[Fact]
	public async Task CreateUseCase_RejectsInvalidReferences()
	{
		var sut = BuildCreateUseCase(new InMemoryRepository([]), StaffRole.Manager);

		var result = await sut.ExecuteAsync(new CreateInventorySubstitutionPolicyCommand(
			"unknown",
			"cake-standard",
			[StaffRole.Manager],
			true,
			NewContext()));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(InventorySubstitutionPolicyConstants.ErrorSourceInvalid);
	}

	[Fact]
	public async Task CreateUseCase_AllowsInactiveRuleWhenActiveDuplicateExists()
	{
		var existing = BuildPolicy("cake-custom", "cake-standard", "Manager", true);
		var repository = new InMemoryRepository([existing]);
		var sut = BuildCreateUseCase(repository, StaffRole.Manager);

		var result = await sut.ExecuteAsync(new CreateInventorySubstitutionPolicyCommand(
			"cake-custom",
			"cake-standard",
			[StaffRole.Manager],
			false,
			NewContext()));

		result.IsSuccess.Should().BeTrue();
		var rows = await repository.ListForManagementAsync();
		rows.Should().HaveCount(2);
		rows.Count(x => x.IsActive).Should().Be(1);
	}

	[Fact]
	public async Task CreateUpdateDelete_EnforcesManagerAuthorization()
	{
		var repository = new InMemoryRepository([]);
		var createUseCase = BuildCreateUseCase(repository, StaffRole.Cashier);

		var createResult = await createUseCase.ExecuteAsync(new CreateInventorySubstitutionPolicyCommand(
			"cake-custom",
			"cake-standard",
			[StaffRole.Manager],
			true,
			NewContext()));
		createResult.IsSuccess.Should().BeFalse();

		var updateUseCase = BuildUpdateUseCase(repository, StaffRole.Cashier);
		var updateResult = await updateUseCase.ExecuteAsync(new UpdateInventorySubstitutionPolicyCommand(
			Guid.NewGuid(),
			"cake-custom",
			"cake-standard",
			[StaffRole.Manager],
			true,
			NewContext()));
		updateResult.IsSuccess.Should().BeFalse();

		var deleteUseCase = BuildDeleteUseCase(repository, StaffRole.Cashier);
		var deleteResult = await deleteUseCase.ExecuteAsync(new DeleteInventorySubstitutionPolicyCommand(Guid.NewGuid(), NewContext()));
		deleteResult.IsSuccess.Should().BeFalse();
	}

	[Fact]
	public async Task UpdateAndDelete_AreIdempotentOnReplayedOperationId()
	{
		var existing = BuildPolicy("cake-custom", "cake-standard", "Manager", true);
		existing.LastOperationId = Guid.NewGuid();
		var repository = new InMemoryRepository([existing]);

		var updateUseCase = BuildUpdateUseCase(repository, StaffRole.Manager);
		var updateResult = await updateUseCase.ExecuteAsync(new UpdateInventorySubstitutionPolicyCommand(
			existing.Id,
			existing.SourceOptionId,
			existing.AllowedSubstituteOptionId,
			[StaffRole.Manager],
			existing.IsActive,
			new OperationContext(existing.LastOperationId, Guid.NewGuid(), null, DateTime.UtcNow)));
		updateResult.IsSuccess.Should().BeTrue();
		updateResult.UserMessage.Should().Be(InventorySubstitutionPolicyConstants.UpdateIdempotentMessage);

		var deleteUseCase = BuildDeleteUseCase(repository, StaffRole.Manager);
		var deleteResult = await deleteUseCase.ExecuteAsync(new DeleteInventorySubstitutionPolicyCommand(
			existing.Id,
			new OperationContext(existing.LastOperationId, Guid.NewGuid(), null, DateTime.UtcNow)));
		deleteResult.IsSuccess.Should().BeTrue();
		deleteResult.UserMessage.Should().Be(InventorySubstitutionPolicyConstants.DeleteIdempotentMessage);
	}

	private static InventorySubstitutionPolicy BuildPolicy(string source, string substitute, string rolesCsv, bool isActive)
	{
		return new InventorySubstitutionPolicy
		{
			Id = Guid.NewGuid(),
			SourceOptionId = source,
			AllowedSubstituteOptionId = substitute,
			AllowedRolesCsv = rolesCsv,
			IsActive = isActive,
			CreatedAtUtc = DateTime.UtcNow,
			UpdatedAtUtc = DateTime.UtcNow,
			CreatedByStaffId = Guid.NewGuid(),
			UpdatedByStaffId = Guid.NewGuid(),
			LastOperationId = Guid.NewGuid(),
		};
	}

	private static OperationContext NewContext() => new(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow);

	private static CreateInventorySubstitutionPolicyUseCase BuildCreateUseCase(InMemoryRepository repository, StaffRole role)
	{
		return new CreateInventorySubstitutionPolicyUseCase(
			repository,
			new TestCurrentSessionService(new CurrentSession(Guid.NewGuid(), role, 1, 1)),
			new AuthorizationPolicyService(),
			new RecordingOperationLogRepository());
	}

	private static UpdateInventorySubstitutionPolicyUseCase BuildUpdateUseCase(InMemoryRepository repository, StaffRole role)
	{
		return new UpdateInventorySubstitutionPolicyUseCase(
			repository,
			new TestCurrentSessionService(new CurrentSession(Guid.NewGuid(), role, 1, 1)),
			new AuthorizationPolicyService(),
			new RecordingOperationLogRepository());
	}

	private static DeleteInventorySubstitutionPolicyUseCase BuildDeleteUseCase(InMemoryRepository repository, StaffRole role)
	{
		return new DeleteInventorySubstitutionPolicyUseCase(
			repository,
			new TestCurrentSessionService(new CurrentSession(Guid.NewGuid(), role, 1, 1)),
			new AuthorizationPolicyService(),
			new RecordingOperationLogRepository());
	}

	private sealed class InMemoryRepository : IInventorySubstitutionPolicyRepository
	{
		private readonly List<InventorySubstitutionPolicy> _rows;

		public InMemoryRepository(IEnumerable<InventorySubstitutionPolicy> rows)
		{
			_rows = rows.ToList();
		}

		public Task<IReadOnlyList<InventorySubstitutionPolicy>> ListForManagementAsync(CancellationToken ct = default)
		{
			return Task.FromResult<IReadOnlyList<InventorySubstitutionPolicy>>(
				_rows
					.OrderBy(x => x.SourceOptionId, StringComparer.Ordinal)
					.ThenBy(x => x.AllowedSubstituteOptionId, StringComparer.Ordinal)
					.ToArray());
		}

		public Task<IReadOnlyList<InventorySubstitutionPolicy>> ListActiveForConstrainedOptionsAsync(IReadOnlyCollection<string> constrainedOptionIds, CancellationToken ct = default)
		{
			return Task.FromResult<IReadOnlyList<InventorySubstitutionPolicy>>(
				_rows
					.Where(x => x.IsActive)
					.Where(x => constrainedOptionIds.Contains(x.SourceOptionId, StringComparer.Ordinal))
					.ToArray());
		}

		public Task<InventorySubstitutionPolicy?> GetByIdAsync(Guid policyId, CancellationToken ct = default)
		{
			return Task.FromResult(_rows.SingleOrDefault(x => x.Id == policyId));
		}

		public Task<InventorySubstitutionPolicy?> GetByLastOperationIdAsync(Guid operationId, CancellationToken ct = default)
		{
			return Task.FromResult(_rows.SingleOrDefault(x => x.LastOperationId == operationId));
		}

		public Task<InventorySubstitutionPolicy?> FindActiveDuplicateAsync(string sourceOptionId, string allowedSubstituteOptionId, string allowedRolesCsv, Guid? excludingPolicyId = null, CancellationToken ct = default)
		{
			var query = _rows
				.Where(x => x.IsActive)
				.Where(x => x.SourceOptionId == sourceOptionId)
				.Where(x => x.AllowedSubstituteOptionId == allowedSubstituteOptionId)
				.Where(x => x.AllowedRolesCsv == allowedRolesCsv);

			if (excludingPolicyId.HasValue)
			{
				query = query.Where(x => x.Id != excludingPolicyId.Value);
			}

			return Task.FromResult(query.FirstOrDefault());
		}

		public Task<InventorySubstitutionPolicy> AddAsync(InventorySubstitutionPolicy policy, CancellationToken ct = default)
		{
			_rows.Add(policy);
			return Task.FromResult(policy);
		}

		public Task UpdateAsync(InventorySubstitutionPolicy policy, CancellationToken ct = default)
		{
			var index = _rows.FindIndex(x => x.Id == policy.Id);
			if (index >= 0)
			{
				_rows[index] = policy;
			}

			return Task.CompletedTask;
		}
	}

	private sealed class RecordingOperationLogRepository : IOperationLogRepository
	{
		public Task<OperationLogEntry> AppendAsync<TPayload>(string eventType, string aggregateId, TPayload payload, OperationContext operationContext, int version = 1, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(new OperationLogEntry
			{
				Id = Guid.NewGuid(),
				EventId = Guid.NewGuid().ToString("N"),
				EventType = eventType,
				AggregateId = aggregateId,
				OperationId = operationContext.OperationId,
				CorrelationId = operationContext.CorrelationId,
				CausationId = operationContext.CausationId,
				OccurredUtc = operationContext.OccurredUtc,
				RecordedUtc = operationContext.OccurredUtc,
				PayloadJson = "{}",
				Version = version,
			});
		}

		public Task<IReadOnlyList<OperationLogEntry>> ListAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult<IReadOnlyList<OperationLogEntry>>([]);
		}

		public Task<IReadOnlyList<OperationLogEntry>> ListByEventTypesAsync(IReadOnlyList<string> eventTypes, CancellationToken cancellationToken = default)
		{
			return Task.FromResult<IReadOnlyList<OperationLogEntry>>([]);
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
