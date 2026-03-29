using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Security;

public sealed record SubmitOverrideResultDto(
	Guid OperationId,
	string ActionKey,
	string TargetReference,
	Guid ApprovedByStaffId,
	StaffRole ApprovedByRole);
