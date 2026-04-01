using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Party;

public sealed record AddOnOptionItemDto(
	string OptionId,
	string DisplayName,
	PartyAddOnType AddOnType,
	bool IsSelected,
	int Quantity,
	long PriceCents,
	bool IsAtRisk,
	string? RiskSeverity,
	string? RiskReason);

public sealed record BookingAddOnOptionsDto(
	Guid BookingId,
	IReadOnlyList<AddOnOptionItemDto> CateringOptions,
	IReadOnlyList<AddOnOptionItemDto> DecorOptions,
	long AddOnTotalAmountCents);

public sealed record BookingRiskIndicatorDto(
	string OptionId,
	string RiskSeverity,
	string RiskReason);

public sealed record BookingAddOnUpdateResultDto(
	Guid BookingId,
	IReadOnlyList<AddOnOptionItemDto> CateringOptions,
	IReadOnlyList<AddOnOptionItemDto> DecorOptions,
	long AddOnTotalAmountCents,
	IReadOnlyList<BookingRiskIndicatorDto> RiskIndicators,
	IReadOnlyList<PartyBookingTimelineMilestoneDto> UpdatedMilestones);
