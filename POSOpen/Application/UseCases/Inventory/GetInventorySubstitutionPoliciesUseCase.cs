using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Results;
using POSOpen.Application.UseCases.Party;

namespace POSOpen.Application.UseCases.Inventory;

public sealed class GetInventorySubstitutionPoliciesUseCase
{
	private readonly IInventorySubstitutionPolicyRepository _policyRepository;
	private readonly ICurrentSessionService _currentSessionService;
	private readonly IAuthorizationPolicyService _authorizationPolicyService;

	public GetInventorySubstitutionPoliciesUseCase(
		IInventorySubstitutionPolicyRepository policyRepository,
		ICurrentSessionService currentSessionService,
		IAuthorizationPolicyService authorizationPolicyService)
	{
		_policyRepository = policyRepository;
		_currentSessionService = currentSessionService;
		_authorizationPolicyService = authorizationPolicyService;
	}

	public async Task<AppResult<IReadOnlyList<InventorySubstitutionPolicyManagementDto>>> ExecuteAsync(
		GetInventorySubstitutionPoliciesQuery query,
		CancellationToken ct = default)
	{
		if (!InventorySubstitutionPolicyAuthorization.IsManagerAuthorized(_currentSessionService, _authorizationPolicyService, out _))
		{
			return AppResult<IReadOnlyList<InventorySubstitutionPolicyManagementDto>>.Failure(
				InventorySubstitutionPolicyConstants.ErrorAuthForbidden,
				InventorySubstitutionPolicyConstants.SafeAuthForbiddenMessage);
		}

		var policies = await _policyRepository.ListForManagementAsync(ct);
		var output = policies
			.Select(policy => new InventorySubstitutionPolicyManagementDto(
				policy.Id,
				policy.SourceOptionId,
				PartyBookingConstants.AddOnOptionDisplayNames.TryGetValue(policy.SourceOptionId, out var sourceName)
					? sourceName
					: policy.SourceOptionId,
				policy.AllowedSubstituteOptionId,
				PartyBookingConstants.AddOnOptionDisplayNames.TryGetValue(policy.AllowedSubstituteOptionId, out var substituteName)
					? substituteName
					: policy.AllowedSubstituteOptionId,
				InventorySubstitutionPolicyRoleCodec.ParseCsv(policy.AllowedRolesCsv),
				policy.IsActive,
				policy.UpdatedAtUtc,
				policy.UpdatedByStaffId))
			.ToArray();

		return AppResult<IReadOnlyList<InventorySubstitutionPolicyManagementDto>>.Success(
			output,
			InventorySubstitutionPolicyConstants.ListLoadedMessage);
	}
}
