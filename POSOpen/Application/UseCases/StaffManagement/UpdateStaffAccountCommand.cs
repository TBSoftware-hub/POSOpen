using POSOpen.Domain.Enums;
using POSOpen.Shared.Operational;

namespace POSOpen.Application.UseCases.StaffManagement;

public sealed record UpdateStaffAccountCommand(
	Guid StaffAccountId,
	string FirstName,
	string LastName,
	string Email,
	StaffRole Role,
	OperationContext Context,
	Guid UpdatedByStaffId);
