using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSOpen.Domain.Entities;
using POSOpen.Infrastructure.Persistence.ValueConverters;

namespace POSOpen.Infrastructure.Persistence.Configurations;

public sealed class InventorySubstitutionPolicyConfiguration : IEntityTypeConfiguration<InventorySubstitutionPolicy>
{
	public void Configure(EntityTypeBuilder<InventorySubstitutionPolicy> builder)
	{
		builder.ToTable("inventory_substitution_policies");
		builder.HasKey(x => x.Id);

		builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
		builder.Property(x => x.SourceOptionId).HasColumnName("source_option_id").HasMaxLength(64).IsRequired();
		builder.Property(x => x.AllowedSubstituteOptionId).HasColumnName("allowed_substitute_option_id").HasMaxLength(64).IsRequired();
		builder.Property(x => x.AllowedRolesCsv).HasColumnName("allowed_roles_csv").HasMaxLength(128).IsRequired();
		builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
		builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasConversion(UtcDateTimeConverter.Instance).IsRequired();
		builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasConversion(UtcDateTimeConverter.Instance).IsRequired();
		builder.Property(x => x.CreatedByStaffId).HasColumnName("created_by_staff_id").IsRequired();
		builder.Property(x => x.UpdatedByStaffId).HasColumnName("updated_by_staff_id").IsRequired();
		builder.Property(x => x.LastOperationId).HasColumnName("last_operation_id").IsRequired();

		builder.HasIndex(x => new { x.SourceOptionId, x.AllowedSubstituteOptionId })
			.HasDatabaseName("ix_inventory_sub_policies_source_substitute");

		builder.HasIndex(x => x.LastOperationId)
			.HasDatabaseName("ix_inventory_sub_policies_last_operation_id");

		builder.HasIndex(x => new { x.SourceOptionId, x.AllowedSubstituteOptionId, x.AllowedRolesCsv, x.IsActive })
			.IsUnique()
			.HasDatabaseName("ux_inventory_sub_policies_active_combo")
			.HasFilter("is_active = 1");
	}
}
