using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Application.UseCases.Party;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Inventory;

public sealed class GetAllowedSubstitutesUseCase
{
	private readonly IInventorySubstitutionPolicyProvider _policyProvider;

	public GetAllowedSubstitutesUseCase(IInventorySubstitutionPolicyProvider policyProvider)
	{
		_policyProvider = policyProvider;
	}

	public async Task<AppResult<IReadOnlyList<AllowedSubstituteOptionDto>>> ExecuteAsync(
		GetAllowedSubstitutesQuery query,
		CancellationToken ct = default)
	{
		if (query.ConstrainedOptionIds.Count == 0)
		{
			return AppResult<IReadOnlyList<AllowedSubstituteOptionDto>>.Success(
				[],
				PartyBookingConstants.InventorySubstitutesLoadedMessage);
		}

		var rules = await _policyProvider.GetAllowedSubstitutesAsync(query.Role, query.ConstrainedOptionIds, ct);
		var output = rules
			.Where(x => x.IsActive)
			.OrderBy(x => x.SourceOptionId, StringComparer.Ordinal)
			.ThenBy(x => x.AllowedSubstituteOptionId, StringComparer.Ordinal)
			.Select(x => new AllowedSubstituteOptionDto(
				x.SourceOptionId,
				x.AllowedSubstituteOptionId,
				PartyBookingConstants.AddOnOptionDisplayNames.TryGetValue(x.AllowedSubstituteOptionId, out var displayName)
					? displayName
					: x.AllowedSubstituteOptionId))
			.ToArray();

		return AppResult<IReadOnlyList<AllowedSubstituteOptionDto>>.Success(output, PartyBookingConstants.InventorySubstitutesLoadedMessage);
	}
}

public sealed record GetAllowedSubstitutesQuery(
	Guid BookingId,
	StaffRole Role,
	IReadOnlyList<string> ConstrainedOptionIds);
