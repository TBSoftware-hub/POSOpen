using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Results;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.StaffManagement;

public sealed class DeactivateStaffAccountUseCase
{
	private readonly IStaffAccountRepository _staffAccountRepository;
	private readonly IOperationLogRepository _operationLogRepository;
	private readonly ILogger<DeactivateStaffAccountUseCase> _logger;

	public DeactivateStaffAccountUseCase(
		IStaffAccountRepository staffAccountRepository,
		IOperationLogRepository operationLogRepository,
		ILogger<DeactivateStaffAccountUseCase> logger)
	{
		_staffAccountRepository = staffAccountRepository;
		_operationLogRepository = operationLogRepository;
		_logger = logger;
	}

	public async Task<AppResult<bool>> ExecuteAsync(DeactivateStaffAccountCommand command, CancellationToken ct = default)
	{
		var account = await _staffAccountRepository.GetByIdAsync(command.StaffAccountId, ct);
		if (account is null)
		{
			return AppResult<bool>.Failure("STAFF_NOT_FOUND", "Staff account not found.");
		}

		if (account.Status == StaffAccountStatus.Inactive)
		{
			return AppResult<bool>.Failure("STAFF_ALREADY_INACTIVE", "This account is already inactive.");
		}

		account.Status = StaffAccountStatus.Inactive;
		account.UpdatedAtUtc = command.Context.OccurredUtc;
		account.UpdatedByStaffId = command.UpdatedByStaffId;

		await _staffAccountRepository.UpdateAsync(account, ct);

		await _operationLogRepository.AppendAsync(
			"StaffAccountDeactivated",
			account.Id.ToString(),
			new
			{
				actorStaffId = command.UpdatedByStaffId,
				targetReference = account.Id.ToString(),
				actionType = "StaffAccountDeactivated",
				account.Email,
				operationId = command.Context.OperationId,
				occurredUtc = command.Context.OccurredUtc
			},
			command.Context,
			version: 1,
			cancellationToken: ct);

		_logger.LogInformation("Staff account {StaffAccountId} deactivated.", account.Id);
		return AppResult<bool>.Success(true, "Staff account deactivated.");
	}
}
