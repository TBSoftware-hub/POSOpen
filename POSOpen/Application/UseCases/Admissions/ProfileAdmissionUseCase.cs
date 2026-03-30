using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Results;
using POSOpen.Application.Security;
using POSOpen.Domain.Entities;

namespace POSOpen.Application.UseCases.Admissions;

public sealed class ProfileAdmissionUseCase
{
	private static readonly string[] RequiredFieldKeys = ["firstName", "lastName", "phone"];

	private readonly IFamilyProfileRepository _familyProfileRepository;
	private readonly ICurrentSessionService _currentSessionService;
	private readonly IAuthorizationPolicyService _authorizationPolicyService;
	private readonly ILogger<ProfileAdmissionUseCase> _logger;

	public ProfileAdmissionUseCase(
		IFamilyProfileRepository familyProfileRepository,
		ICurrentSessionService currentSessionService,
		IAuthorizationPolicyService authorizationPolicyService,
		ILogger<ProfileAdmissionUseCase> logger)
	{
		_familyProfileRepository = familyProfileRepository;
		_currentSessionService = currentSessionService;
		_authorizationPolicyService = authorizationPolicyService;
		_logger = logger;
	}

	public async Task<AppResult<ProfileAdmissionDraftDto>> InitializeAsync(
		InitializeProfileAdmissionDraftQuery query,
		CancellationToken ct = default)
	{
		var sessionResult = RequireAuthorizedSession();
		if (!sessionResult.IsSuccess)
		{
			return AppResult<ProfileAdmissionDraftDto>.Failure(
				sessionResult.ErrorCode!,
				sessionResult.UserMessage);
		}

		if (query.FamilyId is null)
		{
			var newDraft = new ProfileAdmissionDraftDto(
				null,
				false,
				query.Hint,
				string.Empty,
				string.Empty,
				string.Empty,
				null,
				RequiredFieldKeys);

			return AppResult<ProfileAdmissionDraftDto>.Success(newDraft, "Collect required profile fields to continue admission.");
		}

		try
		{
			var profile = await _familyProfileRepository.GetByIdAsync(query.FamilyId.Value, ct);
			if (profile is null)
			{
				return AppResult<ProfileAdmissionDraftDto>.Failure(
					ProfileAdmissionConstants.ErrorProfileNotFound,
					ProfileAdmissionConstants.SafeProfileNotFoundMessage);
			}

			var draft = new ProfileAdmissionDraftDto(
				profile.Id,
				true,
				query.Hint,
				profile.PrimaryContactFirstName,
				profile.PrimaryContactLastName,
				profile.Phone,
				profile.Email,
				CollectMissingRequiredFields(profile.PrimaryContactFirstName, profile.PrimaryContactLastName, profile.Phone));

			return AppResult<ProfileAdmissionDraftDto>.Success(draft, "Complete missing required fields to continue admission.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to initialize profile draft for family {FamilyId}.", query.FamilyId);
			return AppResult<ProfileAdmissionDraftDto>.Failure(
				ProfileAdmissionConstants.ErrorProfileSaveFailed,
				ProfileAdmissionConstants.SafeProfileSaveFailedMessage);
		}
	}

	public async Task<AppResult<SubmitProfileAdmissionResultDto>> SubmitAsync(
		SubmitProfileAdmissionCommand command,
		CancellationToken ct = default)
	{
		var sessionResult = RequireAuthorizedSession();
		if (!sessionResult.IsSuccess || sessionResult.Payload is null)
		{
			return AppResult<SubmitProfileAdmissionResultDto>.Failure(
				sessionResult.ErrorCode!,
				sessionResult.UserMessage);
		}

		var firstName = command.FirstName.Trim();
		var lastName = command.LastName.Trim();
		var phone = NormalizePhone(command.Phone);
		var email = string.IsNullOrWhiteSpace(command.Email) ? null : command.Email.Trim();

		if (CollectMissingRequiredFields(firstName, lastName, phone).Count > 0 || (email is not null && !email.Contains('@', StringComparison.Ordinal)))
		{
			return AppResult<SubmitProfileAdmissionResultDto>.Failure(
				ProfileAdmissionConstants.ErrorProfileRequiredFieldsMissing,
				ProfileAdmissionConstants.SafeRequiredFieldsMissingMessage);
		}

		try
		{
			if (command.FamilyId is null)
			{
				var profile = FamilyProfile.Create(
					Guid.NewGuid(),
					firstName,
					lastName,
					phone,
					email,
					sessionResult.Payload.StaffId,
					DateTime.UtcNow);

				await _familyProfileRepository.AddAsync(profile, ct);
				return AppResult<SubmitProfileAdmissionResultDto>.Success(
					new SubmitProfileAdmissionResultDto(profile.Id),
					"Profile created. Continuing admission.");
			}

			var existing = await _familyProfileRepository.GetByIdAsync(command.FamilyId.Value, ct);
			if (existing is null)
			{
				return AppResult<SubmitProfileAdmissionResultDto>.Failure(
					ProfileAdmissionConstants.ErrorProfileNotFound,
					ProfileAdmissionConstants.SafeProfileNotFoundMessage);
			}

			existing.PrimaryContactFirstName = firstName;
			existing.PrimaryContactLastName = lastName;
			existing.Phone = phone;
			existing.Email = email;
			existing.UpdatedAtUtc = DateTime.UtcNow;

			await _familyProfileRepository.UpdateAsync(existing, ct);
			return AppResult<SubmitProfileAdmissionResultDto>.Success(
				new SubmitProfileAdmissionResultDto(existing.Id),
				"Profile completed. Continuing admission.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to submit profile admission for family {FamilyId}.", command.FamilyId);
			return AppResult<SubmitProfileAdmissionResultDto>.Failure(
				ProfileAdmissionConstants.ErrorProfileSaveFailed,
				ProfileAdmissionConstants.SafeProfileSaveFailedMessage);
		}
	}

	private AppResult<CurrentSession> RequireAuthorizedSession()
	{
		var session = _currentSessionService.GetCurrent();
		if (session is null)
		{
			return AppResult<CurrentSession>.Failure(
				ProfileAdmissionConstants.ErrorAuthForbidden,
				ProfileAdmissionConstants.SafeAuthForbiddenMessage);
		}

		if (!_authorizationPolicyService.HasPermission(session.Role, RolePermissions.AdmissionsLookup))
		{
			return AppResult<CurrentSession>.Failure(
				ProfileAdmissionConstants.ErrorAuthForbidden,
				ProfileAdmissionConstants.SafeAuthForbiddenMessage);
		}

		return AppResult<CurrentSession>.Success(session, "Authorized");
	}

	private static IReadOnlyList<string> CollectMissingRequiredFields(string firstName, string lastName, string phone)
	{
		var missing = new List<string>(3);
		if (string.IsNullOrWhiteSpace(firstName))
		{
			missing.Add("firstName");
		}

		if (string.IsNullOrWhiteSpace(lastName))
		{
			missing.Add("lastName");
		}

		if (string.IsNullOrWhiteSpace(phone))
		{
			missing.Add("phone");
		}

		return missing;
	}

	private static string NormalizePhone(string phone)
	{
		var digits = phone.Where(char.IsDigit).ToArray();
		return digits.Length > 0 ? new string(digits) : phone.Trim();
	}
}
