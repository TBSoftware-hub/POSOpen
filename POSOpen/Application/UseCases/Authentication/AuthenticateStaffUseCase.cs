using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Authentication;

public sealed class AuthenticateStaffUseCase
{
	private const string AuthSucceededEvent = "StaffAuthenticationSucceeded";
	private const string AuthDeniedEvent = "StaffAuthenticationDenied";

	private readonly IStaffAccountRepository _staffAccountRepository;
	private readonly IOperationLogRepository _operationLogRepository;
	private readonly IPasswordHasher _passwordHasher;
	private readonly IAppStateService _appStateService;
	private readonly ILogger<AuthenticateStaffUseCase> _logger;

	public AuthenticateStaffUseCase(
		IStaffAccountRepository staffAccountRepository,
		IOperationLogRepository operationLogRepository,
		IPasswordHasher passwordHasher,
		IAppStateService appStateService,
		ILogger<AuthenticateStaffUseCase> logger)
	{
		_staffAccountRepository = staffAccountRepository;
		_operationLogRepository = operationLogRepository;
		_passwordHasher = passwordHasher;
		_appStateService = appStateService;
		_logger = logger;
	}

	public async Task<AppResult<AuthenticationResultDto>> ExecuteAsync(AuthenticateStaffCommand command, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.Password))
		{
			return AppResult<AuthenticationResultDto>.Failure(
				AuthenticationConstants.ErrorInvalidCredentials,
				AuthenticationConstants.SafeSignInFailureMessage);
		}

		try
		{
			var account = await _staffAccountRepository.GetByNormalizedEmailForAuthenticationAsync(command.Email, ct);
			if (account is null)
			{
				await AppendDeniedEventAsync(
					aggregateId: NormalizeEmail(command.Email),
					reason: AuthenticationConstants.ErrorInvalidCredentials,
					lockedUntilUtc: null,
					command,
					ct);
				return AppResult<AuthenticationResultDto>.Failure(
					AuthenticationConstants.ErrorInvalidCredentials,
					AuthenticationConstants.SafeSignInFailureMessage);
			}

			if (account.LockedUntilUtc.HasValue && account.LockedUntilUtc.Value > command.Context.OccurredUtc)
			{
				await AppendDeniedEventAsync(
					aggregateId: account.Id.ToString(),
					reason: AuthenticationConstants.ErrorAccountLocked,
					lockedUntilUtc: account.LockedUntilUtc,
					command,
					ct);
				return AppResult<AuthenticationResultDto>.Failure(
					AuthenticationConstants.ErrorAccountLocked,
					AuthenticationConstants.SafeSignInFailureMessage);
			}

			if (!_passwordHasher.Verify(command.Password, account.PasswordHash, account.PasswordSalt))
			{
				var failedAccount = await _staffAccountRepository.RecordFailedSignInAttemptAsync(
					account.Id,
					command.Context.OccurredUtc,
					AuthenticationConstants.LockoutThreshold,
					AuthenticationConstants.LockoutDuration,
					ct);

				var failureCode = failedAccount?.LockedUntilUtc is not null && failedAccount.LockedUntilUtc > command.Context.OccurredUtc
					? AuthenticationConstants.ErrorAccountLocked
					: AuthenticationConstants.ErrorInvalidCredentials;

				await AppendDeniedEventAsync(
					aggregateId: account.Id.ToString(),
					reason: failureCode,
					lockedUntilUtc: failedAccount?.LockedUntilUtc,
					command,
					ct);

				return AppResult<AuthenticationResultDto>.Failure(
					failureCode,
					AuthenticationConstants.SafeSignInFailureMessage);
			}

			if (account.Status != StaffAccountStatus.Active)
			{
				await AppendDeniedEventAsync(
					aggregateId: account.Id.ToString(),
					reason: AuthenticationConstants.ErrorAccountInactive,
					lockedUntilUtc: account.LockedUntilUtc,
					command,
					ct);

				return AppResult<AuthenticationResultDto>.Failure(
					AuthenticationConstants.ErrorAccountInactive,
					AuthenticationConstants.SafeSignInFailureMessage);
			}

			var resetAccount = await _staffAccountRepository.RecordSuccessfulSignInAsync(account.Id, command.Context.OccurredUtc, ct);
			if (resetAccount is null)
			{
				_logger.LogWarning("Sign-in could not reset account metadata because account {AccountId} was not found.", account.Id);
				return AppResult<AuthenticationResultDto>.Failure(
					AuthenticationConstants.ErrorSignInUnavailable,
					AuthenticationConstants.SafeSignInUnavailableMessage);
			}

			var sessionVersion = _appStateService.SessionVersion + 1;
			_appStateService.SetCurrentSession(resetAccount.Id, resetAccount.Role, sessionVersion);

			var nextRoute = ResolveRoleRoute(resetAccount.Role);
			var result = new AuthenticationResultDto(resetAccount.Id, resetAccount.Role, sessionVersion, nextRoute);

			await _operationLogRepository.AppendAsync(
				AuthSucceededEvent,
				resetAccount.Id.ToString(),
				new
				{
					staffId = resetAccount.Id,
					role = resetAccount.Role.ToString(),
					sessionVersion,
					nextRoute,
					operationId = command.Context.OperationId,
					occurredUtc = command.Context.OccurredUtc
				},
				command.Context,
				version: 1,
				cancellationToken: ct);

			return AppResult<AuthenticationResultDto>.Success(result, "Sign-in successful.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Unhandled sign-in exception.");
			return AppResult<AuthenticationResultDto>.Failure(
				AuthenticationConstants.ErrorSignInUnavailable,
				AuthenticationConstants.SafeSignInUnavailableMessage);
		}
	}

	private async Task AppendDeniedEventAsync(
		string aggregateId,
		string reason,
		DateTime? lockedUntilUtc,
		AuthenticateStaffCommand command,
		CancellationToken ct)
	{
		await _operationLogRepository.AppendAsync(
			AuthDeniedEvent,
			aggregateId,
			new
			{
				reason,
				lockedUntilUtc,
				operationId = command.Context.OperationId,
				occurredUtc = command.Context.OccurredUtc
			},
			command.Context,
			version: 1,
			cancellationToken: ct);
	}

	private static string ResolveRoleRoute(StaffRole role)
	{
		return role switch
		{
			StaffRole.Owner => "staff/list",
			StaffRole.Admin => "staff/list",
			StaffRole.Manager => "manager/operations",
			_ => "home"
		};
	}

	private static string NormalizeEmail(string email)
	{
		return email.Trim().ToLowerInvariant();
	}
}
