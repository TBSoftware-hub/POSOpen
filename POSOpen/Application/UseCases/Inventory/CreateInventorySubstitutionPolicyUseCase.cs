using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Results;
using POSOpen.Application.Security;
using POSOpen.Application.UseCases.Party;
using POSOpen.Domain.Entities;

namespace POSOpen.Application.UseCases.Inventory;

public sealed class CreateInventorySubstitutionPolicyUseCase
{
	private readonly IInventorySubstitutionPolicyRepository _policyRepository;
	private readonly ICurrentSessionService _currentSessionService;
	private readonly IAuthorizationPolicyService _authorizationPolicyService;
	private readonly IOperationLogRepository _operationLogRepository;

	public CreateInventorySubstitutionPolicyUseCase(
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
		CreateInventorySubstitutionPolicyCommand command,
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

		var duplicate = await _policyRepository.FindActiveDuplicateAsync(
			sourceOptionId,
			substituteOptionId,
			allowedRolesCsv,
			null,
			ct);
		if (duplicate is not null && command.IsActive)
		{
			return AppResult<InventorySubstitutionPolicyManagementDto>.Failure(
				InventorySubstitutionPolicyConstants.ErrorDuplicate,
				InventorySubstitutionPolicyConstants.SafeDuplicateMessage);
		}

		var occurredUtc = DateTime.SpecifyKind(command.Context.OccurredUtc, DateTimeKind.Utc);
		var policy = new InventorySubstitutionPolicy
		{
			Id = Guid.NewGuid(),
			SourceOptionId = sourceOptionId,
			AllowedSubstituteOptionId = substituteOptionId,
			AllowedRolesCsv = allowedRolesCsv,
			IsActive = command.IsActive,
			CreatedAtUtc = occurredUtc,
			UpdatedAtUtc = occurredUtc,
			CreatedByStaffId = session!.StaffId,
			UpdatedByStaffId = session.StaffId,
			LastOperationId = command.Context.OperationId,
		};

		await _policyRepository.AddAsync(policy, ct);

		await _operationLogRepository.AppendAsync(
			SecurityAuditEventTypes.InventorySubstitutionPolicyCreated,
			policy.Id.ToString(),
			new
			{
				actorStaffId = session.StaffId,
				actorRole = session.Role.ToString(),
				targetReference = policy.Id.ToString(),
				actionType = SecurityAuditEventTypes.InventorySubstitutionPolicyCreated,
				sourceOptionId = policy.SourceOptionId,
				allowedSubstituteOptionId = policy.AllowedSubstituteOptionId,
				allowedRoles = normalizedRoles.Select(static x => x.ToString()).ToArray(),
				isActive = policy.IsActive,
				operationId = command.Context.OperationId,
				occurredUtc = occurredUtc,
			},
			command.Context,
			version: 1,
			cancellationToken: ct);

		return AppResult<InventorySubstitutionPolicyManagementDto>.Success(
			Map(policy, normalizedRoles),
			InventorySubstitutionPolicyConstants.CreatedMessage);
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

	private static InventorySubstitutionPolicyManagementDto Map(
		InventorySubstitutionPolicy policy,
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
