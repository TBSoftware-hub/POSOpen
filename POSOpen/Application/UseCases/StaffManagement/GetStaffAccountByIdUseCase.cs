using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Results;

namespace POSOpen.Application.UseCases.StaffManagement;

public sealed class GetStaffAccountByIdUseCase
{
	private readonly IStaffAccountRepository _staffAccountRepository;

	public GetStaffAccountByIdUseCase(IStaffAccountRepository staffAccountRepository)
	{
		_staffAccountRepository = staffAccountRepository;
	}

	public async Task<AppResult<StaffAccountDto>> ExecuteAsync(Guid staffAccountId, CancellationToken ct = default)
	{
		var account = await _staffAccountRepository.GetByIdAsync(staffAccountId, ct);
		if (account is null)
		{
			return AppResult<StaffAccountDto>.Failure("STAFF_NOT_FOUND", "Staff account not found.");
		}

		var dto = new StaffAccountDto(
			account.Id,
			account.FirstName,
			account.LastName,
			account.Email,
			account.Role,
			account.Status,
			account.CreatedAtUtc,
			account.UpdatedAtUtc,
			account.CreatedByStaffId,
			account.UpdatedByStaffId);

		return AppResult<StaffAccountDto>.Success(dto, "Staff account loaded.");
	}
}
