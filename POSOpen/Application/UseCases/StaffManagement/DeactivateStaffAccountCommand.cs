using POSOpen.Shared.Operational;

namespace POSOpen.Application.UseCases.StaffManagement;

public sealed record DeactivateStaffAccountCommand(
	Guid StaffAccountId,
	OperationContext Context,
	Guid UpdatedByStaffId);
