using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.StaffManagement;

public sealed record StaffAccountDto(
	Guid Id,
	string FirstName,
	string LastName,
	string Email,
	StaffRole Role,
	StaffAccountStatus Status,
	DateTime CreatedAtUtc,
	DateTime UpdatedAtUtc,
	Guid? CreatedByStaffId,
	Guid? UpdatedByStaffId);
