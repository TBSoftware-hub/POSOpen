namespace POSOpen.Application.Security;

using POSOpen.Domain.Enums;

public sealed record CurrentSession(
	Guid StaffId,
	StaffRole Role,
	long SessionVersion,
	long PermissionSnapshotVersion)
{
	public bool HasStalePermissionSnapshot => SessionVersion != PermissionSnapshotVersion;
}
