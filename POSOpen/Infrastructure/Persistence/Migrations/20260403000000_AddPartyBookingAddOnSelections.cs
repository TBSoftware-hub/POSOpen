using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POSOpen.Infrastructure.Persistence;

#nullable disable

namespace POSOpen.Infrastructure.Persistence.Migrations
{
	[DbContext(typeof(PosOpenDbContext))]
	[Migration("20260403000000_AddPartyBookingAddOnSelections")]
	public partial class AddPartyBookingAddOnSelections : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<Guid>(
				name: "last_add_on_update_operation_id",
				table: "party_bookings",
				type: "TEXT",
				nullable: true);

			migrationBuilder.CreateTable(
				name: "party_booking_add_on_selections",
				columns: table => new
				{
					id = table.Column<Guid>(type: "TEXT", nullable: false),
					booking_id = table.Column<Guid>(type: "TEXT", nullable: false),
					add_on_type = table.Column<int>(type: "INTEGER", nullable: false),
					option_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
					quantity = table.Column<int>(type: "INTEGER", nullable: false),
					selected_at_utc = table.Column<string>(type: "TEXT", nullable: false),
					selection_operation_id = table.Column<Guid>(type: "TEXT", nullable: false),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_party_booking_add_on_selections", x => x.id);
					table.ForeignKey(
						name: "FK_party_booking_add_on_selections_party_bookings_booking_id",
						column: x => x.booking_id,
						principalTable: "party_bookings",
						principalColumn: "id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "ix_party_booking_add_on_sel_booking_id",
				table: "party_booking_add_on_selections",
				column: "booking_id");

			migrationBuilder.CreateIndex(
				name: "ix_party_booking_add_on_sel_operation_id",
				table: "party_booking_add_on_selections",
				column: "selection_operation_id");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "party_booking_add_on_selections");

			migrationBuilder.DropColumn(
				name: "last_add_on_update_operation_id",
				table: "party_bookings");
		}
	}
}
