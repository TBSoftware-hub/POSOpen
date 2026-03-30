namespace POSOpen.Application.UseCases.Admissions;

public sealed record CompleteAdmissionCheckInCommand(
	Guid FamilyId,
	long AmountCents,
	string CurrencyCode);
