using POSOpen.Domain.Entities;

namespace POSOpen.Application.Abstractions.Repositories;

public interface IStaffAccountRepository
{
	Task AddAsync(StaffAccount account, CancellationToken ct = default);

	Task<StaffAccount?> GetByIdAsync(Guid id, CancellationToken ct = default);

	Task<StaffAccount?> GetByEmailAsync(string email, CancellationToken ct = default);

	Task<IReadOnlyList<StaffAccount>> ListActiveAsync(CancellationToken ct = default);

	Task UpdateAsync(StaffAccount account, CancellationToken ct = default);
}
