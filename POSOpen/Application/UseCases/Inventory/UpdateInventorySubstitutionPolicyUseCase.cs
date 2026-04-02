using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Results;
using POSOpen.Application.Security;
using POSOpen.Application.UseCases.Party;

namespace POSOpen.Application.UseCases.Inventory;

public sealed class UpdateInventorySubstitutionPolicyUseCase
{
	private readonly IInventorySubstitutionPolicyRepository _policyRepository;
	private readonly ICurrentSessionService _currentSessionService;
	private readonly IAuthorizationPolicyService _authorizationPolicyService;
	private readonly IOperationLogRepository _operationLogRepository;

	public UpdateInventorySubstitutionPolicyUseCase(
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

	public async Task<AppResult<InventorySubstitutionPolicyManagementDto>> ExecuteAsync(
		UpdateInventorySubstitutionPolicyCommand command,
		CancellationToken ct = default)
	{
		if (!InventorySubstitutionPolicyAuthorization.IsManagerAuthorized(_currentSessionService, _authorizationPolicyService, out var session))
		{
			return AppResult<InventorySubstitutionPolicyManagementDto>.Failure(
				InventorySubstitutionPolicyConstants.ErrorAuthForbidden,
				InventorySubstitutionPolicyConstants.SafeAuthForbiddenMessage);
		}

		var sourceOptionId = command.SourceOptionId.Trim();
		var substituteOptionId = command.AllowedSubstituteOptionId.Trim();
		var allowedRolesCsv = InventorySubstitutionPolicyRoleCodec.NormalizeToCsv(command.AllowedRoles);
		var normalizedRoles = InventorySubstitutionPolicyRoleCodec.ParseCsv(allowedRolesCsv);

		var validation = Validate(sourceOptionId, substituteOptionId, normalizedRoles);
		if (!validation.IsSuccess)
		{
			return validation;
		}

		var policy = await _policyRepository.GetByIdAsync(command.PolicyId, ct);
		if (policy is null)
		{
			return AppResult<InventorySubstitutionPolicyManagementDto>.Failure(
				InventorySubstitutionPolicyConstants.ErrorPolicyNotFound,
				InventorySubstitutionPolicyConstants.SafePolicyNotFoundMessage);
		}

		if (policy.LastOperationId == command.Context.OperationId)
		{
			return AppResult<InventorySubstitutionPolicyManagementDto>.Success(
				Map(policy, InventorySubstitutionPolicyRoleCodec.ParseCsv(policy.AllowedRolesCsv)),
				InventorySubstitutionPolicyConstants.UpdateIdempotentMessage);
		}

		var duplicate = await _policyRepository.FindActiveDuplicateAsync(
			sourceOptionId,
			substituteOptionId,
			allowedRolesCsv,
			command.PolicyId,
			ct);
		if (duplicate is not null && command.IsActive)
		{
			return AppResult<InventorySubstitutionPolicyManagementDto>.Failure(
				InventorySubstitutionPolicyConstants.ErrorDuplicate,
				InventorySubstitutionPolicyConstants.SafeDuplicateMessage);
		}

		var changedFields = new List<string>();
		TrackChange(changedFields, policy.SourceOptionId, sourceOptionId, nameof(policy.SourceOptionId));
		TrackChange(changedFields, policy.AllowedSubstituteOptionId, substituteOptionId, nameof(policy.AllowedSubstituteOptionId));
		TrackChange(changedFields, policy.AllowedRolesCsv, allowedRolesCsv, nameof(policy.AllowedRolesCsv));
		TrackChange(changedFields, policy.IsActive, command.IsActive, nameof(policy.IsActive));

		policy.SourceOptionId = sourceOptionId;
		policy.AllowedSubstituteOptionId = substituteOptionId;
		policy.AllowedRolesCsv = allowedRolesCsv;
		policy.IsActive = command.IsActive;
		policy.UpdatedAtUtc = DateTime.SpecifyKind(command.Context.OccurredUtc, DateTimeKind.Utc);
		policy.UpdatedByStaffId = session!.StaffId;
		policy.LastOperationId = command.Context.OperationId;

		await _policyRepository.UpdateAsync(policy, ct);

		await _operationLogRepository.AppendAsync(
			SecurityAuditEventTypes.InventorySubstitutionPolicyUpdated,
			policy.Id.ToString(),
			new
			{
				actorStaffId = session.StaffId,
				actorRole = session.Role.ToString(),
				targetReference = policy.Id.ToString(),
				actionType = SecurityAuditEventTypes.InventorySubstitutionPolicyUpdated,
				updatedFields = changedFields,
				sourceOptionId = policy.SourceOptionId,
				allowedSubstituteOptionId = policy.AllowedSubstituteOptionId,
				allowedRoles = normalizedRoles.Select(static x => x.ToString()).ToArray(),
				isActive = policy.IsActive,
				operationId = command.Context.OperationId,
				occurredUtc = policy.UpdatedAtUtc,
			},
			command.Context,
			version: 1,
			cancellationToken: ct);

		return AppResult<InventorySubstitutionPolicyManagementDto>.Success(
			Map(policy, normalizedRoles),
			InventorySubstitutionPolicyConstants.UpdatedMessage);
	}

	private static AppResult<InventorySubstitutionPolicyManagementDto> Validate(
		string sourceOptionId,
		string substituteOptionId,
		IReadOnlyList<POSOpen.Domain.Enums.StaffRole> allowedRoles)
	{
		if (string.IsNullOrWhiteSpace(sourceOptionId))
		{
			return AppResult<InventorySubstitutionPolicyManagementDto>.Failure(
				InventorySubstitutionPolicyConstants.ErrorSourceRequired,
				InventorySubstitutionPolicyConstants.SafeSourceRequiredMessage);
		}

		if (string.IsNullOrWhiteSpace(substituteOptionId))
		{
			return AppResult<InventorySubstitutionPolicyManagementDto>.Failure(
				InventorySubstitutionPolicyConstants.ErrorSubstituteRequired,
				InventorySubstitutionPolicyConstants.SafeSubstituteRequiredMessage);
		}

		if (allowedRoles.Count == 0)
		{
			return AppResult<InventorySubstitutionPolicyManagementDto>.Failure(
				InventorySubstitutionPolicyConstants.ErrorRoleRequired,
				InventorySubstitutionPolicyConstants.SafeRoleRequiredMessage);
		}

		if (!PartyBookingConstants.AddOnOptionDisplayNames.ContainsKey(sourceOptionId))
		{
			return AppResult<InventorySubstitutionPolicyManagementDto>.Failure(
				InventorySubstitutionPolicyConstants.ErrorSourceInvalid,
				InventorySubstitutionPolicyConstants.SafeSourceInvalidMessage);
		}

		if (!PartyBookingConstants.AddOnOptionDisplayNames.ContainsKey(substituteOptionId))
		{
			return AppResult<InventorySubstitutionPolicyManagementDto>.Failure(
				InventorySubstitutionPolicyConstants.ErrorSubstituteInvalid,
				InventorySubstitutionPolicyConstants.SafeSubstituteInvalidMessage);
		}

		if (string.Equals(sourceOptionId, substituteOptionId, StringComparison.Ordinal))
		{
			return AppResult<InventorySubstitutionPolicyManagementDto>.Failure(
				InventorySubstitutionPolicyConstants.ErrorSelfReference,
				InventorySubstitutionPolicyConstants.SafeSelfReferenceMessage);
		}

		return AppResult<InventorySubstitutionPolicyManagementDto>.Success(default!, string.Empty);
	}

	private static void TrackChange<T>(List<string> changedFields, T before, T after, string fieldName)
	{
		if (!EqualityComparer<T>.Default.Equals(before, after))
		{
			changedFields.Add(fieldName);
		}
	}

	private static InventorySubstitutionPolicyManagementDto Map(
		POSOpen.Domain.Entities.InventorySubstitutionPolicy policy,
		IReadOnlyList<POSOpen.Domain.Enums.StaffRole> allowedRoles)
	{
		return new InventorySubstitutionPolicyManagementDto(
			policy.Id,
			policy.SourceOptionId,
			PartyBookingConstants.AddOnOptionDisplayNames.TryGetValue(policy.SourceOptionId, out var sourceName)
				? sourceName
				: policy.SourceOptionId,
			policy.AllowedSubstituteOptionId,
			PartyBookingConstants.AddOnOptionDisplayNames.TryGetValue(policy.AllowedSubstituteOptionId, out var substituteName)
				? substituteName
				: policy.AllowedSubstituteOptionId,
			allowedRoles,
			policy.IsActive,
			policy.UpdatedAtUtc,
			policy.UpdatedByStaffId);
	}
}
