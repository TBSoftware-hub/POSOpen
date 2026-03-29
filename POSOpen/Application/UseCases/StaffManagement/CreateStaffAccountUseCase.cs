using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.StaffManagement;

public sealed class CreateStaffAccountUseCase
{
	public const int MinPasswordLength = 8;

	private readonly IStaffAccountRepository _staffAccountRepository;
	private readonly IOperationLogRepository _operationLogRepository;
	private readonly IPasswordHasher _passwordHasher;
	private readonly ILogger<CreateStaffAccountUseCase> _logger;

	public CreateStaffAccountUseCase(
		IStaffAccountRepository staffAccountRepository,
		IOperationLogRepository operationLogRepository,
		IPasswordHasher passwordHasher,
		ILogger<CreateStaffAccountUseCase> logger)
	{
		_staffAccountRepository = staffAccountRepository;
		_operationLogRepository = operationLogRepository;
		_passwordHasher = passwordHasher;
		_logger = logger;
	}

	public async Task<AppResult<StaffAccountDto>> ExecuteAsync(CreateStaffAccountCommand command, CancellationToken ct = default)
	{
		var validation = Validate(command);
		if (!validation.IsSuccess)
		{
			return validation;
		}

		var normalizedEmail = NormalizeEmail(command.Email);
		var existing = await _staffAccountRepository.GetByEmailAsync(normalizedEmail, ct);
		if (existing is not null)
		{
			_logger.LogInformation("Staff account creation rejected due to duplicate email.");
			return AppResult<StaffAccountDto>.Failure(
				"STAFF_EMAIL_CONFLICT",
				"An account with this email address already exists.");
		}

		var (hash, salt) = _passwordHasher.Hash(command.PlaintextPassword);
		var account = StaffAccount.Create(
			Guid.NewGuid(),
			command.FirstName.Trim(),
			command.LastName.Trim(),
			normalizedEmail,
			hash,
			salt,
			command.Role,
			StaffAccountStatus.Active,
			command.Context.OccurredUtc,
			command.Context.OccurredUtc,
			command.CreatedByStaffId,
			command.CreatedByStaffId);

		await _staffAccountRepository.AddAsync(account, ct);

		await _operationLogRepository.AppendAsync(
			"StaffAccountCreated",
			account.Id.ToString(),
			new
			{
				account.Id,
				account.Email,
				account.Role
			},
			command.Context,
			version: 1,
			cancellationToken: ct);

		return AppResult<StaffAccountDto>.Success(Map(account), "Staff account created.");
	}

	private static AppResult<StaffAccountDto> Validate(CreateStaffAccountCommand command)
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

		if (string.IsNullOrWhiteSpace(command.PlaintextPassword))
		{
			return AppResult<StaffAccountDto>.Failure("STAFF_VALIDATION_PASSWORD_REQUIRED", "Password is required.");
		}

		if (command.PlaintextPassword.Length < MinPasswordLength)
		{
			return AppResult<StaffAccountDto>.Failure("STAFF_VALIDATION_PASSWORD_TOO_SHORT", $"Password must be at least {MinPasswordLength} characters.");
		}

		return AppResult<StaffAccountDto>.Success(default!, string.Empty);
	}

	private static string NormalizeEmail(string email)
	{
		return email.Trim().ToLowerInvariant();
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
