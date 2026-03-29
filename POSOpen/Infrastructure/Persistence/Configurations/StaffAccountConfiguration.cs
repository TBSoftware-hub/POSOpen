using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSOpen.Domain.Entities;
using POSOpen.Infrastructure.Persistence.ValueConverters;

namespace POSOpen.Infrastructure.Persistence.Configurations;

public sealed class StaffAccountConfiguration : IEntityTypeConfiguration<StaffAccount>
{
	public void Configure(EntityTypeBuilder<StaffAccount> builder)
	{
		builder.ToTable("staff_accounts");
		builder.HasKey(account => account.Id);
		builder.Property(account => account.Id).HasColumnName("id").ValueGeneratedNever();
		builder.Property(account => account.FirstName).HasColumnName("first_name").IsRequired().HasMaxLength(100);
		builder.Property(account => account.LastName).HasColumnName("last_name").IsRequired().HasMaxLength(100);
		builder.Property(account => account.Email).HasColumnName("email").IsRequired().HasMaxLength(254);
		builder.Property(account => account.PasswordHash).HasColumnName("password_hash").IsRequired().HasMaxLength(512);
		builder.Property(account => account.PasswordSalt).HasColumnName("password_salt").IsRequired().HasMaxLength(128);
		builder.Property(account => account.Role).HasColumnName("role").HasConversion<int>().IsRequired();
		builder.Property(account => account.Status).HasColumnName("status").HasConversion<int>().IsRequired();
		builder.Property(account => account.FailedLoginAttempts).HasColumnName("failed_login_attempts").HasDefaultValue(0).IsRequired();
		builder.Property(account => account.LockedUntilUtc).HasColumnName("locked_until_utc").HasConversion(NullableUtcDateTimeConverter.Instance);
		builder.Property(account => account.CreatedAtUtc).HasColumnName("created_at_utc").HasConversion(UtcDateTimeConverter.Instance).IsRequired();
		builder.Property(account => account.UpdatedAtUtc).HasColumnName("updated_at_utc").HasConversion(UtcDateTimeConverter.Instance).IsRequired();
		builder.Property(account => account.CreatedByStaffId).HasColumnName("created_by_staff_id");
		builder.Property(account => account.UpdatedByStaffId).HasColumnName("updated_by_staff_id");
		builder.HasIndex(account => account.Email).IsUnique().HasDatabaseName("ix_staff_accounts_email");
	}
}
