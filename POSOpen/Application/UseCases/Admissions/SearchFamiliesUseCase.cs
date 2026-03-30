using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Results;
using POSOpen.Application.Security;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Admissions;

public sealed class SearchFamiliesUseCase
{
	private readonly IFamilyProfileRepository _familyProfileRepository;
	private readonly ICurrentSessionService _currentSessionService;
	private readonly IAuthorizationPolicyService _authorizationPolicyService;
	private readonly ILogger<SearchFamiliesUseCase> _logger;

	public SearchFamiliesUseCase(
		IFamilyProfileRepository familyProfileRepository,
		ICurrentSessionService currentSessionService,
		IAuthorizationPolicyService authorizationPolicyService,
		ILogger<SearchFamiliesUseCase> logger)
	{
		_familyProfileRepository = familyProfileRepository;
		_currentSessionService = currentSessionService;
		_authorizationPolicyService = authorizationPolicyService;
		_logger = logger;
	}

	public async Task<AppResult<IReadOnlyList<FamilySearchResultDto>>> ExecuteAsync(
		SearchFamiliesQuery query,
		CancellationToken ct = default)
	{
		var session = _currentSessionService.GetCurrent();
		if (session is null)
		{
			return AppResult<IReadOnlyList<FamilySearchResultDto>>.Failure(
				SearchFamiliesConstants.ErrorAuthForbidden,
				SearchFamiliesConstants.SafeAuthForbiddenMessage);
		}

		if (!_authorizationPolicyService.HasPermission(session.Role, RolePermissions.AdmissionsLookup))
		{
			_logger.LogWarning("Family lookup denied for staff {StaffId} in role {Role}.", session.StaffId, session.Role);
			return AppResult<IReadOnlyList<FamilySearchResultDto>>.Failure(
				SearchFamiliesConstants.ErrorAuthForbidden,
				SearchFamiliesConstants.SafeAuthForbiddenMessage);
		}

		try
		{
			if (query.Mode == FamilyLookupMode.Text)
			{
				var normalizedQuery = query.Query.Trim();
				if (normalizedQuery.Length < 2)
				{
					return AppResult<IReadOnlyList<FamilySearchResultDto>>.Failure(
						SearchFamiliesConstants.ErrorLookupQueryTooShort,
						SearchFamiliesConstants.SafeLookupQueryTooShortMessage);
				}

				var matches = await _familyProfileRepository.SearchAsync(normalizedQuery, ct);
				var payload = matches.Select(Map).ToList();
				return AppResult<IReadOnlyList<FamilySearchResultDto>>.Success(
					payload,
					payload.Count == 0 ? SearchFamiliesConstants.EmptyLookupMessage : $"{payload.Count} matching families found.");
			}

			var normalizedToken = query.Query.Trim();
			if (string.IsNullOrWhiteSpace(normalizedToken))
			{
				return AppResult<IReadOnlyList<FamilySearchResultDto>>.Success(
					Array.Empty<FamilySearchResultDto>(),
					SearchFamiliesConstants.EmptyLookupMessage);
			}

			var profile = await _familyProfileRepository.GetByScanTokenAsync(normalizedToken, ct);
			if (profile is null)
			{
				return AppResult<IReadOnlyList<FamilySearchResultDto>>.Success(
					Array.Empty<FamilySearchResultDto>(),
					SearchFamiliesConstants.EmptyLookupMessage);
			}

			return AppResult<IReadOnlyList<FamilySearchResultDto>>.Success(
				new[] { Map(profile) },
				"Family found.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Family lookup failed for query mode {Mode}.", query.Mode);
			return AppResult<IReadOnlyList<FamilySearchResultDto>>.Failure(
				SearchFamiliesConstants.ErrorLookupUnavailable,
				SearchFamiliesConstants.SafeLookupUnavailableMessage);
		}
	}

	private static FamilySearchResultDto Map(FamilyProfile profile)
	{
		return new FamilySearchResultDto(
			profile.Id,
			$"{profile.PrimaryContactLastName}, {profile.PrimaryContactFirstName}",
			profile.Phone,
			profile.WaiverStatus,
			ToWaiverStatusLabel(profile.WaiverStatus),
			false);
	}

	private static string ToWaiverStatusLabel(WaiverStatus status)
	{
		return status switch
		{
			WaiverStatus.Valid => "Waiver OK",
			WaiverStatus.Pending => "Waiver Pending",
			WaiverStatus.Expired => "Waiver Expired",
			_ => "No Waiver"
		};
	}
}
