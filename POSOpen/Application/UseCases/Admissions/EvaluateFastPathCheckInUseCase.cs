using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Results;
using POSOpen.Application.Security;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Admissions;

public sealed class EvaluateFastPathCheckInUseCase
{
	private readonly IFamilyProfileRepository _familyProfileRepository;
	private readonly ICurrentSessionService _currentSessionService;
	private readonly IAuthorizationPolicyService _authorizationPolicyService;
	private readonly ILogger<EvaluateFastPathCheckInUseCase> _logger;

	public EvaluateFastPathCheckInUseCase(
		IFamilyProfileRepository familyProfileRepository,
		ICurrentSessionService currentSessionService,
		IAuthorizationPolicyService authorizationPolicyService,
		ILogger<EvaluateFastPathCheckInUseCase> logger)
	{
		_familyProfileRepository = familyProfileRepository;
		_currentSessionService = currentSessionService;
		_authorizationPolicyService = authorizationPolicyService;
		_logger = logger;
	}

	public async Task<AppResult<FastPathCheckInEvaluationResultDto>> ExecuteAsync(
		EvaluateFastPathCheckInQuery query,
		CancellationToken ct = default)
	{
		var session = _currentSessionService.GetCurrent();
		if (session is null)
		{
			return AppResult<FastPathCheckInEvaluationResultDto>.Failure(
				EvaluateFastPathCheckInConstants.ErrorAuthForbidden,
				EvaluateFastPathCheckInConstants.SafeAuthForbiddenMessage);
		}

		if (!_authorizationPolicyService.HasPermission(session.Role, RolePermissions.AdmissionsLookup))
		{
			_logger.LogWarning(
				"Fast-path check-in denied for staff {StaffId} in role {Role}.",
				session.StaffId,
				session.Role);

			return AppResult<FastPathCheckInEvaluationResultDto>.Failure(
				EvaluateFastPathCheckInConstants.ErrorAuthForbidden,
				EvaluateFastPathCheckInConstants.SafeAuthForbiddenMessage);
		}

		try
		{
			var profile = await _familyProfileRepository.GetByIdAsync(query.FamilyId, ct);
			if (profile is null)
			{
				return AppResult<FastPathCheckInEvaluationResultDto>.Failure(
					EvaluateFastPathCheckInConstants.ErrorFamilyNotFound,
					EvaluateFastPathCheckInConstants.SafeFamilyNotFoundMessage);
			}

			var evaluation = Evaluate(profile, query.IsRefreshRequested);
			return AppResult<FastPathCheckInEvaluationResultDto>.Success(evaluation, evaluation.GuidanceMessage);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Fast-path check-in evaluation failed for family {FamilyId}.", query.FamilyId);
			return AppResult<FastPathCheckInEvaluationResultDto>.Failure(
				EvaluateFastPathCheckInConstants.ErrorFastPathUnavailable,
				EvaluateFastPathCheckInConstants.SafeFastPathUnavailableMessage);
		}
	}

	private static FastPathCheckInEvaluationResultDto Evaluate(FamilyProfile profile, bool isRefreshRequested)
	{
		var state = ToState(profile.WaiverStatus, isRefreshRequested);
		var isEligible = state == FastPathEligibilityState.Allowed;
		return new FastPathCheckInEvaluationResultDto(
			profile.Id,
			$"{profile.PrimaryContactLastName}, {profile.PrimaryContactFirstName}",
			profile.WaiverStatus,
			ToWaiverStatusLabel(profile.WaiverStatus),
			state,
			isEligible,
			ShowRecoveryAction(profile.WaiverStatus),
			state == FastPathEligibilityState.RefreshRequired,
			ToGuidanceMessage(profile.WaiverStatus, state, isRefreshRequested));
	}

	private static FastPathEligibilityState ToState(WaiverStatus status, bool isRefreshRequested)
	{
		if (status == WaiverStatus.Valid)
		{
			return FastPathEligibilityState.Allowed;
		}

		if (status == WaiverStatus.Pending && !isRefreshRequested)
		{
			return FastPathEligibilityState.RefreshRequired;
		}

		return FastPathEligibilityState.Blocked;
	}

	private static bool ShowRecoveryAction(WaiverStatus status)
	{
		return status is WaiverStatus.None or WaiverStatus.Expired or WaiverStatus.Pending;
	}

	private static string ToGuidanceMessage(WaiverStatus waiverStatus, FastPathEligibilityState state, bool isRefreshRequested)
	{
		if (waiverStatus == WaiverStatus.Pending && state == FastPathEligibilityState.Blocked && isRefreshRequested)
		{
			return EvaluateFastPathCheckInConstants.PendingStillBlockedMessage;
		}

		return state switch
		{
			FastPathEligibilityState.Allowed => EvaluateFastPathCheckInConstants.AllowedMessage,
			FastPathEligibilityState.RefreshRequired => EvaluateFastPathCheckInConstants.RefreshRequiredMessage,
			_ => EvaluateFastPathCheckInConstants.BlockedMessage
		};
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
