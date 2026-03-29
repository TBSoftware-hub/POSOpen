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

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(PosOpenDbContext).Assembly);
	}
}