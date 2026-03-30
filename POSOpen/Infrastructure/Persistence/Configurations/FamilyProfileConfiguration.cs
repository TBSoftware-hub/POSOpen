using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSOpen.Domain.Entities;
using POSOpen.Infrastructure.Persistence.ValueConverters;

namespace POSOpen.Infrastructure.Persistence.Configurations;

public sealed class FamilyProfileConfiguration : IEntityTypeConfiguration<FamilyProfile>
{
	public void Configure(EntityTypeBuilder<FamilyProfile> builder)
	{
		builder.ToTable("family_profiles");
		builder.HasKey(profile => profile.Id);

		builder.Property(profile => profile.Id).HasColumnName("id").ValueGeneratedNever();
		builder.Property(profile => profile.PrimaryContactFirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
		builder.Property(profile => profile.PrimaryContactLastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
		builder.Property(profile => profile.Phone).HasColumnName("phone").HasMaxLength(32).IsRequired();
		builder.Property(profile => profile.Email).HasColumnName("email").HasMaxLength(254);
		builder.Property(profile => profile.WaiverStatus).HasColumnName("waiver_status").HasConversion<int>().IsRequired();
		builder.Property(profile => profile.WaiverCompletedAtUtc).HasColumnName("waiver_completed_at_utc").HasConversion(NullableUtcDateTimeConverter.Instance);
		builder.Property(profile => profile.ScanToken).HasColumnName("scan_token").HasMaxLength(64);
		builder.Property(profile => profile.CreatedAtUtc).HasColumnName("created_at_utc").HasConversion(UtcDateTimeConverter.Instance).IsRequired();
		builder.Property(profile => profile.UpdatedAtUtc).HasColumnName("updated_at_utc").HasConversion(UtcDateTimeConverter.Instance).IsRequired();
		builder.Property(profile => profile.CreatedByStaffId).HasColumnName("created_by_staff_id");

		builder.HasIndex(profile => profile.Phone).HasDatabaseName("ix_family_profiles_phone");
		builder.HasIndex(profile => profile.ScanToken).HasDatabaseName("ix_family_profiles_scan_token");
		builder.HasIndex(profile => profile.PrimaryContactLastName).HasDatabaseName("ix_family_profiles_last_name");
	}
}
