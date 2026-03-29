using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Results;
using POSOpen.Application.Security;
using POSOpen.Domain.Entities;

namespace POSOpen.Application.UseCases.StaffManagement;

public sealed class AssignStaffRoleUseCase
{
	private readonly IStaffAccountRepository _staffAccountRepository;
	private readonly IOperationLogRepository _operationLogRepository;
	private readonly IAuthorizationPolicyService _authorizationPolicyService;
	private readonly ICurrentSessionService _currentSessionService;
	private readonly ILogger<AssignStaffRoleUseCase> _logger;

	public AssignStaffRoleUseCase(
		IStaffAccountRepository staffAccountRepository,
		IOperationLogRepository operationLogRepository,
		IAuthorizationPolicyService authorizationPolicyService,
		ICurrentSessionService currentSessionService,
		ILogger<AssignStaffRoleUseCase> logger)
	{
		_staffAccountRepository = staffAccountRepository;
		_operationLogRepository = operationLogRepository;
		_authorizationPolicyService = authorizationPolicyService;
		_currentSessionService = currentSessionService;
		_logger = logger;
	}

	public async Task<AppResult<StaffAccountDto>> ExecuteAsync(AssignStaffRoleCommand command, CancellationToken ct = default)
	{
		var session = _currentSessionService.GetCurrent();
		if (session is null)
		{
			return AppResult<StaffAccountDto>.Failure("AUTH_FORBIDDEN", "You do not have access to this action.");
		}

		if (!_authorizationPolicyService.HasPermission(session.Role, RolePermissions.StaffRoleAssign))
		{
			_logger.LogWarning("Staff role assignment denied for role {Role}.", session.Role);
			return AppResult<StaffAccountDto>.Failure("AUTH_FORBIDDEN", "You do not have access to this action.");
		}

		var account = await _staffAccountRepository.GetByIdAsync(command.StaffAccountId, ct);
		if (account is null)
		{
			return AppResult<StaffAccountDto>.Failure("STAFF_NOT_FOUND", "Staff account not found.");
		}

		if (account.Role == command.Role)
		{
			return AppResult<StaffAccountDto>.Failure("STAFF_ROLE_NO_CHANGE", "Staff account already has this role.");
		}

		var previousRole = account.Role;
		account.Role = command.Role;
		account.UpdatedAtUtc = command.Context.OccurredUtc;
		account.UpdatedByStaffId = session.StaffId;

		await _staffAccountRepository.UpdateAsync(account, ct);
		var sessionVersion = _currentSessionService.IncrementSessionVersion();

		await _operationLogRepository.AppendAsync(
			"StaffRoleAssigned",
			account.Id.ToString(),
			new
			{
				staffAccountId = account.Id,
				previousRole,
				newRole = account.Role,
				changedByStaffId = session.StaffId,
				operationId = command.Context.OperationId,
				occurredUtc = command.Context.OccurredUtc,
				sessionVersion
			},
			command.Context,
			version: 1,
			cancellationToken: ct);

		_logger.LogInformation(
			"Assigned role {NewRole} to staff account {StaffAccountId} by actor {ActorId}.",
			account.Role,
			account.Id,
			session.StaffId);

		return AppResult<StaffAccountDto>.Success(Map(account), "Staff role updated.");
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
