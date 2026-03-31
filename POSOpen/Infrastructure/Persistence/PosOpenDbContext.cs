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

    public DbSet<CartSession> CartSessions => Set<CartSession>();

    public DbSet<CartLineItem> CartLineItems => Set<CartLineItem>();

    public DbSet<CheckoutPaymentAttempt> CheckoutPaymentAttempts => Set<CheckoutPaymentAttempt>();

    public DbSet<RefundRecord> RefundRecords => Set<RefundRecord>();

    public DbSet<PartyBooking> PartyBookings => Set<PartyBooking>();

    public DbSet<ReceiptMetadata> ReceiptMetadata => Set<ReceiptMetadata>();

    public DbSet<TransactionOperation> TransactionOperations => Set<TransactionOperation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PosOpenDbContext).Assembly);
    }
}
