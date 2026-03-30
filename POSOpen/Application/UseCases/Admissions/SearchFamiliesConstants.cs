namespace POSOpen.Application.UseCases.Admissions;

public static class SearchFamiliesConstants
{
	public const string ErrorAuthForbidden = "AUTH_FORBIDDEN";
	public const string ErrorLookupQueryTooShort = "LOOKUP_QUERY_TOO_SHORT";
	public const string ErrorLookupUnavailable = "LOOKUP_UNAVAILABLE";

	public const string SafeAuthForbiddenMessage = "You do not have access to this action.";
	public const string SafeLookupQueryTooShortMessage = "Enter at least 2 characters to search families.";
	public const string SafeLookupUnavailableMessage = "Search is temporarily unavailable. Please try again.";
	public const string EmptyLookupMessage = "No matching families found.";
}
