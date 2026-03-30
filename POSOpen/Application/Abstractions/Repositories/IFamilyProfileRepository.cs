using POSOpen.Domain.Entities;

namespace POSOpen.Application.Abstractions.Repositories;

public interface IFamilyProfileRepository
{
	Task<IReadOnlyList<FamilyProfile>> SearchAsync(string query, CancellationToken ct = default);

	Task<FamilyProfile?> GetByScanTokenAsync(string token, CancellationToken ct = default);

	Task<FamilyProfile?> GetByIdAsync(Guid id, CancellationToken ct = default);

	Task AddAsync(FamilyProfile profile, CancellationToken ct = default);

	Task UpdateAsync(FamilyProfile profile, CancellationToken ct = default);
}
