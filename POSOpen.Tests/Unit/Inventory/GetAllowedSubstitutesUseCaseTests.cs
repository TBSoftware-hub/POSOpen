using FluentAssertions;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.UseCases.Inventory;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Infrastructure.Services;

namespace POSOpen.Tests.Unit.Inventory;

public sealed class GetAllowedSubstitutesUseCaseTests
{
	[Fact]
	public async Task ExecuteAsync_ReturnsRoleFilteredOrderedSubstitutes()
	{
		var repository = new InMemoryInventorySubstitutionPolicyRepository(
		[
			new InventorySubstitutionPolicy
			{
				Id = Guid.NewGuid(),
				SourceOptionId = "cake-custom",
				AllowedSubstituteOptionId = "cake-standard",
				AllowedRolesCsv = "Owner,Admin,Manager",
				IsActive = true,
			},
			new InventorySubstitutionPolicy
			{
				Id = Guid.NewGuid(),
				SourceOptionId = "table-themed",
				AllowedSubstituteOptionId = "table-standard",
				AllowedRolesCsv = "Cashier,Manager",
				IsActive = true,
			},
			new InventorySubstitutionPolicy
			{
				Id = Guid.NewGuid(),
				SourceOptionId = "banner-custom",
				AllowedSubstituteOptionId = "banner-standard",
				AllowedRolesCsv = "Cashier,Manager",
				IsActive = true,
			},
			new InventorySubstitutionPolicy
			{
				Id = Guid.NewGuid(),
				SourceOptionId = "banner-custom",
				AllowedSubstituteOptionId = "banner-standard",
				AllowedRolesCsv = "Cashier",
				IsActive = false,
			}
		]);

		var provider = new RepositoryInventorySubstitutionPolicyProvider(repository);
		var sut = new GetAllowedSubstitutesUseCase(provider);

		var result = await sut.ExecuteAsync(new GetAllowedSubstitutesQuery(
			Guid.NewGuid(),
			StaffRole.Cashier,
			["banner-custom", "cake-custom", "table-themed"]));

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.Select(x => x.SourceOptionId).Should().Equal("banner-custom", "table-themed");
	}

	private sealed class InMemoryInventorySubstitutionPolicyRepository : IInventorySubstitutionPolicyRepository
	{
		private readonly List<InventorySubstitutionPolicy> _rows;

		public InMemoryInventorySubstitutionPolicyRepository(IEnumerable<InventorySubstitutionPolicy> rows)
		{
			_rows = rows.ToList();
		}

		public Task<IReadOnlyList<InventorySubstitutionPolicy>> ListForManagementAsync(CancellationToken ct = default)
		{
			return Task.FromResult<IReadOnlyList<InventorySubstitutionPolicy>>(_rows.ToList());
		}

		public Task<IReadOnlyList<InventorySubstitutionPolicy>> ListActiveForConstrainedOptionsAsync(IReadOnlyCollection<string> constrainedOptionIds, CancellationToken ct = default)
		{
			var output = _rows
				.Where(x => x.IsActive)
				.Where(x => constrainedOptionIds.Contains(x.SourceOptionId, StringComparer.Ordinal))
				.OrderBy(x => x.SourceOptionId, StringComparer.Ordinal)
				.ThenBy(x => x.AllowedSubstituteOptionId, StringComparer.Ordinal)
				.ToArray();

			return Task.FromResult<IReadOnlyList<InventorySubstitutionPolicy>>(output);
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
}
