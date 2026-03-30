namespace POSOpen.Application.UseCases.Admissions;

public sealed record SearchFamiliesQuery(string Query, FamilyLookupMode Mode);

public enum FamilyLookupMode
{
	Text = 1,
	Scan = 2
}
