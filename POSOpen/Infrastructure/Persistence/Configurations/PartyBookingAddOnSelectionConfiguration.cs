using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSOpen.Domain.Entities;
using POSOpen.Infrastructure.Persistence.ValueConverters;

namespace POSOpen.Infrastructure.Persistence.Configurations;

public sealed class PartyBookingAddOnSelectionConfiguration : IEntityTypeConfiguration<PartyBookingAddOnSelection>
{
	public void Configure(EntityTypeBuilder<PartyBookingAddOnSelection> builder)
	{
		builder.ToTable("party_booking_add_on_selections");
		builder.HasKey(x => x.Id);

		builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
		builder.Property(x => x.BookingId).HasColumnName("booking_id").IsRequired();
		builder.Property(x => x.AddOnType).HasColumnName("add_on_type").HasConversion<int>().IsRequired();
		builder.Property(x => x.OptionId).HasColumnName("option_id").HasMaxLength(64).IsRequired();
		builder.Property(x => x.Quantity).HasColumnName("quantity").IsRequired();
		builder.Property(x => x.SelectedAtUtc).HasColumnName("selected_at_utc").HasConversion(UtcDateTimeConverter.Instance).IsRequired();
		builder.Property(x => x.SelectionOperationId).HasColumnName("selection_operation_id").IsRequired();

		builder.HasIndex(x => x.BookingId)
			.HasDatabaseName("ix_party_booking_add_on_sel_booking_id");

		builder.HasIndex(x => x.SelectionOperationId)
			.HasDatabaseName("ix_party_booking_add_on_sel_operation_id");
	}
}
