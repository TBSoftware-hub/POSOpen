using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Results;
using POSOpen.Domain.Entities;

namespace POSOpen.Application.UseCases.StaffManagement;

public sealed class UpdateStaffAccountUseCase
{
	private readonly IStaffAccountRepository _staffAccountRepository;
	private readonly IOperationLogRepository _operationLogRepository;
	private readonly ILogger<UpdateStaffAccountUseCase> _logger;

	public UpdateStaffAccountUseCase(
		IStaffAccountRepository staffAccountRepository,
		IOperationLogRepository operationLogRepository,
		ILogger<UpdateStaffAccountUseCase> logger)
	{
		_staffAccountRepository = staffAccountRepository;
		_operationLogRepository = operationLogRepository;
		_logger = logger;
	}

	public async Task<AppResult<StaffAccountDto>> ExecuteAsync(UpdateStaffAccountCommand command, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(command.FirstName))
		{
			return AppResult<StaffAccountDto>.Failure("STAFF_VALIDATION_FIRST_NAME_REQUIRED", "First name is required.");
		}

		if (string.IsNullOrWhiteSpace(command.LastName))
		{
			return AppResult<StaffAccountDto>.Failure("STAFF_VALIDATION_LAST_NAME_REQUIRED", "Last name is required.");
		}

		if (string.IsNullOrWhiteSpace(command.Email))
		{
			return AppResult<StaffAccountDto>.Failure("STAFF_VALIDATION_EMAIL_REQUIRED", "Email is required.");
		}

		var account = await _staffAccountRepository.GetByIdAsync(command.StaffAccountId, ct);
		if (account is null)
		{
			return AppResult<StaffAccountDto>.Failure("STAFF_NOT_FOUND", "Staff account not found.");
		}

		var normalizedEmail = command.Email.Trim().ToLowerInvariant();
		if (!string.Equals(account.Email, normalizedEmail, StringComparison.Ordinal))
		{
			var emailOwner = await _staffAccountRepository.GetByEmailAsync(normalizedEmail, ct);
			if (emailOwner is not null && emailOwner.Id != account.Id)
			{
				return AppResult<StaffAccountDto>.Failure(
					"STAFF_EMAIL_CONFLICT",
					"An account with this email address already exists.");
			}
		}

		var changedFields = new List<string>();
		if (!string.Equals(account.FirstName, command.FirstName.Trim(), StringComparison.Ordinal))
		{
			account.FirstName = command.FirstName.Trim();
			changedFields.Add("FirstName");
		}

		if (!string.Equals(account.LastName, command.LastName.Trim(), StringComparison.Ordinal))
		{
			account.LastName = command.LastName.Trim();
			changedFields.Add("LastName");
		}

		if (!string.Equals(account.Email, normalizedEmail, StringComparison.Ordinal))
		{
			account.Email = normalizedEmail;
			changedFields.Add("Email");
		}

		account.UpdatedAtUtc = command.Context.OccurredUtc;
		account.UpdatedByStaffId = command.UpdatedByStaffId;
		changedFields.Add("UpdatedAtUtc");
		changedFields.Add("UpdatedByStaffId");

		await _staffAccountRepository.UpdateAsync(account, ct);

		await _operationLogRepository.AppendAsync(
			"StaffAccountUpdated",
			account.Id.ToString(),
			new
			{
				actorStaffId = command.UpdatedByStaffId,
				targetReference = account.Id.ToString(),
				actionType = "StaffAccountUpdated",
				updatedFields = changedFields,
				operationId = command.Context.OperationId,
				occurredUtc = command.Context.OccurredUtc
			},
			command.Context,
			version: 1,
			cancellationToken: ct);

		_logger.LogInformation("Staff account {StaffAccountId} updated.", account.Id);
		return AppResult<StaffAccountDto>.Success(Map(account), "Staff account updated.");
	}

	private static StaffAccountDto Map(StaffAccount account)
	{
		return new StaffAccountDto(
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
	}
}
