using POSOpen.Domain.Enums;
using POSOpen.Shared.Operational;

namespace POSOpen.Application.UseCases.StaffManagement;

public sealed record AssignStaffRoleCommand(
	Guid StaffAccountId,
	StaffRole Role,
	OperationContext Context);
