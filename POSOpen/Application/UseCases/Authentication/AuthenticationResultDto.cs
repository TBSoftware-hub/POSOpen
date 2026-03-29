using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Authentication;

public sealed record AuthenticationResultDto(
	Guid StaffId,
	StaffRole Role,
	long SessionVersion,
	string NextRoute);
