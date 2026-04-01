using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POSOpen.Infrastructure.Persistence;

#nullable disable

namespace POSOpen.Infrastructure.Persistence.Migrations
{
	[DbContext(typeof(PosOpenDbContext))]
	[Migration("20260404000000_AddInventoryReservations")]
	public partial class AddInventoryReservations : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<Guid>(
				name: "last_inventory_release_operation_id",
				table: "party_bookings",
				type: "TEXT",
				nullable: true);

			migrationBuilder.AddColumn<Guid>(
				name: "last_inventory_reserve_operation_id",
				table: "party_bookings",
				type: "TEXT",
				nullable: true);

			migrationBuilder.CreateTable(
				name: "inventory_reservations",
				columns: table => new
				{
					reservation_id = table.Column<Guid>(type: "TEXT", nullable: false),
					booking_id = table.Column<Guid>(type: "TEXT", nullable: false),
					option_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
					quantity_reserved = table.Column<int>(type: "INTEGER", nullable: false),
					reservation_state = table.Column<int>(type: "INTEGER", nullable: false),
					reserved_at_utc = table.Column<string>(type: "TEXT", nullable: false),
					released_at_utc = table.Column<string>(type: "TEXT", nullable: true),
					reservation_operation_id = table.Column<Guid>(type: "TEXT", nullable: false),
					release_operation_id = table.Column<Guid>(type: "TEXT", nullable: true),
					release_reason_code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_inventory_reservations", x => x.reservation_id);
					table.ForeignKey(
						name: "FK_inventory_reservations_party_bookings_booking_id",
						column: x => x.booking_id,
						principalTable: "party_bookings",
						principalColumn: "id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "ix_inventory_reservations_booking_id",
				table: "inventory_reservations",
				column: "booking_id");

			migrationBuilder.CreateIndex(
				name: "ix_inventory_reservations_booking_state",
				table: "inventory_reservations",
				columns: new[] { "booking_id", "reservation_state" });

			migrationBuilder.CreateIndex(
				name: "ix_inventory_reservations_option_state",
				table: "inventory_reservations",
				columns: new[] { "option_id", "reservation_state" });

			migrationBuilder.CreateIndex(
				name: "ix_inventory_reservations_release_operation_id",
				table: "inventory_reservations",
				column: "release_operation_id",
				filter: "release_operation_id IS NOT NULL");

			migrationBuilder.CreateIndex(
				name: "ix_inventory_reservations_reservation_operation_id",
				table: "inventory_reservations",
				column: "reservation_operation_id");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "inventory_reservations");

			migrationBuilder.DropColumn(
				name: "last_inventory_release_operation_id",
				table: "party_bookings");

			migrationBuilder.DropColumn(
				name: "last_inventory_reserve_operation_id",
				table: "party_bookings");
		}
	}
}
