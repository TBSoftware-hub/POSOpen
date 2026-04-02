namespace POSOpen.Domain.Entities;

public sealed class InventorySubstitutionPolicy
{
	public Guid Id { get; set; }
	public string SourceOptionId { get; set; } = string.Empty;
	public string AllowedSubstituteOptionId { get; set; } = string.Empty;
	public string AllowedRolesCsv { get; set; } = string.Empty;
	public bool IsActive { get; set; }
	public DateTime CreatedAtUtc { get; set; }
	public DateTime UpdatedAtUtc { get; set; }
	public Guid CreatedByStaffId { get; set; }
	public Guid UpdatedByStaffId { get; set; }
	public Guid LastOperationId { get; set; }
}
