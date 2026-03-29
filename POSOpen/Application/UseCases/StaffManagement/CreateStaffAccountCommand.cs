using POSOpen.Domain.Enums;
using POSOpen.Shared.Operational;

namespace POSOpen.Application.UseCases.StaffManagement;

public sealed record CreateStaffAccountCommand(
	string FirstName,
	string LastName,
	string Email,
	string PlaintextPassword,
	StaffRole Role,
	OperationContext Context,
	Guid? CreatedByStaffId);
