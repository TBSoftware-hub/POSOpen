using Microsoft.EntityFrameworkCore;
using POSOpen.Domain.Entities;

namespace POSOpen.Infrastructure.Persistence;

public sealed class PosOpenDbContext : DbContext
{
	public PosOpenDbContext(DbContextOptions<PosOpenDbContext> options)
		: base(options)
	{
	}

	public DbSet<OperationLogEntry> OperationLogEntries => Set<OperationLogEntry>();

	public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

	public DbSet<AdmissionCheckInRecord> AdmissionCheckInRecords => Set<AdmissionCheckInRecord>();

	public DbSet<FamilyProfile> FamilyProfiles => Set<FamilyProfile>();

	public DbSet<StaffAccount> StaffAccounts => Set<StaffAccount>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(PosOpenDbContext).Assembly);
	}
}