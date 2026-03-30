namespace POSOpen.Application.UseCases.Admissions;

public sealed record InitializeProfileAdmissionDraftQuery(Guid? FamilyId, string? Hint);

public sealed record ProfileAdmissionDraftDto(
	Guid? FamilyId,
	bool IsExistingProfile,
	string? Hint,
	string FirstName,
	string LastName,
	string Phone,
	string? Email,
	IReadOnlyList<string> MissingRequiredFields);

public sealed record SubmitProfileAdmissionCommand(
	Guid? FamilyId,
	string FirstName,
	string LastName,
	string Phone,
	string? Email);

public sealed record SubmitProfileAdmissionResultDto(Guid FamilyId);
