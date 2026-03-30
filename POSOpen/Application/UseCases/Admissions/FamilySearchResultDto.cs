using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Admissions;

public sealed record FamilySearchResultDto(
	Guid Id,
	string DisplayName,
	string Phone,
	WaiverStatus WaiverStatus,
	string WaiverStatusLabel,
	bool HasPaymentOnFile);
