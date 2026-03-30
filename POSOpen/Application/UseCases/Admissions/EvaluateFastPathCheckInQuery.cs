namespace POSOpen.Application.UseCases.Admissions;

public sealed record EvaluateFastPathCheckInQuery(Guid FamilyId, bool IsRefreshRequested = false);
