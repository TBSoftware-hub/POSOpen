using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Results;
using POSOpen.Application.Security;

namespace POSOpen.Application.UseCases.Inventory;

public sealed class DeleteInventorySubstitutionPolicyUseCase
{
	private readonly IInventorySubstitutionPolicyRepository _policyRepository;
	private readonly ICurrentSessionService _currentSessionService;
	private readonly IAuthorizationPolicyService _authorizationPolicyService;
	private readonly IOperationLogRepository _operationLogRepository;

	public DeleteInventorySubstitutionPolicyUseCase(
		IInventorySubstitutionPolicyRepository policyRepository,
		ICurrentSessionService currentSessionService,
		IAuthorizationPolicyService authorizationPolicyService,
		IOperationLogRepository operationLogRepository)
	{
		_policyRepository = policyRepository;
		_currentSessionService = currentSessionService;
		_authorizationPolicyService = authorizationPolicyService;
		_operationLogRepository = operationLogRepository;
	}

	public async Task<AppResult<bool>> ExecuteAsync(DeleteInventorySubstitutionPolicyCommand command, CancellationToken ct = default)
	{
		if (!InventorySubstitutionPolicyAuthorization.IsManagerAuthorized(_currentSessionService, _authorizationPolicyService, out var session))
		{
			return AppResult<bool>.Failure(
				InventorySubstitutionPolicyConstants.ErrorAuthForbidden,
				InventorySubstitutionPolicyConstants.SafeAuthForbiddenMessage);
		}

		var policy = await _policyRepository.GetByIdAsync(command.PolicyId, ct);
		if (policy is null)
		{
			return AppResult<bool>.Failure(
				InventorySubstitutionPolicyConstants.ErrorPolicyNotFound,
				InventorySubstitutionPolicyConstants.SafePolicyNotFoundMessage);
		}

		if (policy.LastOperationId == command.Context.OperationId)
		{
			return AppResult<bool>.Success(true, InventorySubstitutionPolicyConstants.DeleteIdempotentMessage);
		}

		if (!policy.IsActive)
		{
			return AppResult<bool>.Success(true, InventorySubstitutionPolicyConstants.AlreadyInactiveMessage);
		}

		policy.IsActive = false;
		policy.UpdatedAtUtc = DateTime.SpecifyKind(command.Context.OccurredUtc, DateTimeKind.Utc);
		policy.UpdatedByStaffId = session!.StaffId;
		policy.LastOperationId = command.Context.OperationId;
		await _policyRepository.UpdateAsync(policy, ct);

		await _operationLogRepository.AppendAsync(
			SecurityAuditEventTypes.InventorySubstitutionPolicyDeleted,
			policy.Id.ToString(),
			new
			{
				actorStaffId = session.StaffId,
				actorRole = session.Role.ToString(),
				targetReference = policy.Id.ToString(),
				actionType = SecurityAuditEventTypes.InventorySubstitutionPolicyDeleted,
				sourceOptionId = policy.SourceOptionId,
				allowedSubstituteOptionId = policy.AllowedSubstituteOptionId,
				allowedRoles = InventorySubstitutionPolicyRoleCodec.ParseCsv(policy.AllowedRolesCsv).Select(static x => x.ToString()).ToArray(),
				operationId = command.Context.OperationId,
				occurredUtc = policy.UpdatedAtUtc,
			},
			command.Context,
			version: 1,
			cancellationToken: ct);

		return AppResult<bool>.Success(true, InventorySubstitutionPolicyConstants.DeletedMessage);
	}
}
