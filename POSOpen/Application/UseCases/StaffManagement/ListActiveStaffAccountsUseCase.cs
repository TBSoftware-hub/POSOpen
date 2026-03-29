using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Results;

namespace POSOpen.Application.UseCases.StaffManagement;

public sealed class ListActiveStaffAccountsUseCase
{
	private readonly IStaffAccountRepository _staffAccountRepository;

	public ListActiveStaffAccountsUseCase(IStaffAccountRepository staffAccountRepository)
	{
		_staffAccountRepository = staffAccountRepository;
	}

	public async Task<AppResult<IReadOnlyList<StaffAccountDto>>> ExecuteAsync(CancellationToken ct = default)
	{
		var accounts = await _staffAccountRepository.ListActiveAsync(ct);
		var dtos = accounts
			.Select(account => new StaffAccountDto(
				account.Id,
				account.FirstName,
				account.LastName,
				account.Email,
				account.Role,
				account.Status,
				account.CreatedAtUtc,
				account.UpdatedAtUtc,
				account.CreatedByStaffId,
				account.UpdatedByStaffId))
			.ToList();

		return AppResult<IReadOnlyList<StaffAccountDto>>.Success(dtos, "Staff accounts loaded.");
	}
}
