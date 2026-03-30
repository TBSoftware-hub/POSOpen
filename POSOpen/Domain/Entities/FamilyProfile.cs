using POSOpen.Domain.Enums;

namespace POSOpen.Domain.Entities;

public sealed class FamilyProfile
{
	private FamilyProfile()
	{
	}

	public static FamilyProfile Create(
		Guid id,
		string firstName,
		string lastName,
		string phone,
		string? email,
		Guid? createdByStaffId,
		DateTime clockUtc)
	{
		return new FamilyProfile
		{
			Id = id,
			PrimaryContactFirstName = firstName.Trim(),
			PrimaryContactLastName = lastName.Trim(),
			Phone = phone.Trim(),
			Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
			WaiverStatus = WaiverStatus.None,
			WaiverCompletedAtUtc = null,
			ScanToken = Guid.NewGuid().ToString("N"),
			CreatedAtUtc = clockUtc,
			UpdatedAtUtc = clockUtc,
			CreatedByStaffId = createdByStaffId
		};
	}

	public Guid Id { get; init; }

	public required string PrimaryContactFirstName { get; set; }

	public required string PrimaryContactLastName { get; set; }

	public required string Phone { get; set; }

	public string? Email { get; set; }

	public WaiverStatus WaiverStatus { get; set; }

	public DateTime? WaiverCompletedAtUtc { get; set; }

	public string? ScanToken { get; set; }

	public DateTime CreatedAtUtc { get; init; }

	public DateTime UpdatedAtUtc { get; set; }

	public Guid? CreatedByStaffId { get; init; }
}
