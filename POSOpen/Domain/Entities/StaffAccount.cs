using POSOpen.Domain.Enums;

namespace POSOpen.Domain.Entities;

public sealed class StaffAccount
{
	private StaffAccount()
	{
	}

	public static StaffAccount Create(
		Guid id,
		string firstName,
		string lastName,
		string email,
		string passwordHash,
		string passwordSalt,
		StaffRole role,
		StaffAccountStatus status,
		DateTime createdAtUtc,
		DateTime updatedAtUtc,
		Guid? createdByStaffId,
		Guid? updatedByStaffId)
	{
		return new StaffAccount
		{
			Id = id,
			FirstName = firstName,
			LastName = lastName,
			Email = email,
			PasswordHash = passwordHash,
			PasswordSalt = passwordSalt,
			Role = role,
			Status = status,
			FailedLoginAttempts = 0,
			LockedUntilUtc = null,
			CreatedAtUtc = createdAtUtc,
			UpdatedAtUtc = updatedAtUtc,
			CreatedByStaffId = createdByStaffId,
			UpdatedByStaffId = updatedByStaffId
		};
	}

	public Guid Id { get; init; }

	public required string FirstName { get; set; }

	public required string LastName { get; set; }

	public required string Email { get; set; }

	public required string PasswordHash { get; set; }

	public required string PasswordSalt { get; set; }

	public StaffRole Role { get; set; }

	public StaffAccountStatus Status { get; set; }

	public int FailedLoginAttempts { get; set; }

	public DateTime? LockedUntilUtc { get; set; }

	public DateTime CreatedAtUtc { get; init; }

	public DateTime UpdatedAtUtc { get; set; }

	public Guid? CreatedByStaffId { get; init; }

	public Guid? UpdatedByStaffId { get; set; }
}
