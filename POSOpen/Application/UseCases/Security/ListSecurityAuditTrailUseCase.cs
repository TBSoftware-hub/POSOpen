using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Results;
using POSOpen.Application.Security;
using POSOpen.Shared.Operational;

namespace POSOpen.Application.UseCases.Security;

public sealed class ListSecurityAuditTrailUseCase
{
	private readonly IOperationLogRepository _operationLogRepository;
	private readonly ICurrentSessionService _currentSessionService;
	private readonly IAuthorizationPolicyService _authorizationPolicyService;
	private readonly ILogger<ListSecurityAuditTrailUseCase> _logger;

	public ListSecurityAuditTrailUseCase(
		IOperationLogRepository operationLogRepository,
		ICurrentSessionService currentSessionService,
		IAuthorizationPolicyService authorizationPolicyService,
		ILogger<ListSecurityAuditTrailUseCase> logger)
	{
		_operationLogRepository = operationLogRepository;
		_currentSessionService = currentSessionService;
		_authorizationPolicyService = authorizationPolicyService;
		_logger = logger;
	}

	public async Task<AppResult<IReadOnlyList<SecurityAuditRecordDto>>> ExecuteAsync(
		OperationContext context,
		CancellationToken ct = default)
	{
		var session = _currentSessionService.GetCurrent();
		if (session is null)
		{
			_logger.LogWarning("Security audit trail access attempted without an active session.");
			return AppResult<IReadOnlyList<SecurityAuditRecordDto>>.Failure(
				ListSecurityAuditTrailConstants.ErrorAuthForbidden,
				ListSecurityAuditTrailConstants.SafeAuthForbiddenMessage);
		}

		if (!_authorizationPolicyService.HasPermission(session.Role, RolePermissions.SecurityAuditRead))
		{
			_logger.LogWarning(
				"Security audit trail access denied. StaffId={StaffId}, Role={Role}.",
				session.StaffId,
				session.Role);

			await _operationLogRepository.AppendAsync(
				SecurityAuditEventTypes.SecurityAuditAccessDenied,
				session.StaffId.ToString(),
				new
				{
					actorStaffId = session.StaffId,
					actorRole = session.Role.ToString(),
					targetReference = "security-audit-trail",
					actionType = SecurityAuditEventTypes.SecurityAuditAccessDenied,
					operationId = context.OperationId,
					occurredUtc = context.OccurredUtc
				},
				context,
				version: 1,
				cancellationToken: ct);

			return AppResult<IReadOnlyList<SecurityAuditRecordDto>>.Failure(
				ListSecurityAuditTrailConstants.ErrorAuthForbidden,
				ListSecurityAuditTrailConstants.SafeAuthForbiddenMessage);
		}

		try
		{
			var entries = await _operationLogRepository.ListByEventTypesAsync(
				SecurityAuditEventTypes.SecurityCriticalScope,
				ct);

			var records = entries
				.Select(e => new SecurityAuditRecordDto(
					Id: e.Id,
					EventType: e.EventType,
					AggregateId: e.AggregateId,
					OperationId: e.OperationId,
					CorrelationId: e.CorrelationId,
					OccurredUtc: e.OccurredUtc,
					RecordedUtc: e.RecordedUtc,
					PayloadJson: e.PayloadJson))
				.ToList();

			_logger.LogInformation(
				"Security audit trail retrieved. StaffId={StaffId}, RecordCount={Count}.",
				session.StaffId,
				records.Count);

			return AppResult<IReadOnlyList<SecurityAuditRecordDto>>.Success(
				records,
				$"{records.Count} audit record(s) found.");
		}
		catch (Exception ex)
		{
			_logger.LogError(
				ex,
				"Failed to retrieve security audit trail. StaffId={StaffId}.",
				session.StaffId);

			return AppResult<IReadOnlyList<SecurityAuditRecordDto>>.Failure(
				ListSecurityAuditTrailConstants.ErrorAuditTrailUnavailable,
				ListSecurityAuditTrailConstants.SafeAuditTrailUnavailableMessage);
		}
	}
}
