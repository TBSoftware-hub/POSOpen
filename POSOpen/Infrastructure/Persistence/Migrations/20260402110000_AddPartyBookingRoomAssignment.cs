using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POSOpen.Infrastructure.Persistence;

#nullable disable

namespace POSOpen.Infrastructure.Persistence.Migrations
{
	[DbContext(typeof(PosOpenDbContext))]
	[Migration("20260402110000_AddPartyBookingRoomAssignment")]
	public partial class AddPartyBookingRoomAssignment : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<string>(
				name: "assigned_room_id",
				table: "party_bookings",
				type: "TEXT",
				maxLength: 64,
				nullable: true);

			migrationBuilder.AddColumn<string>(
				name: "room_assigned_at_utc",
				table: "party_bookings",
				type: "TEXT",
				nullable: true);

			migrationBuilder.AddColumn<Guid>(
				name: "room_assignment_operation_id",
				table: "party_bookings",
				type: "TEXT",
				nullable: true);

			migrationBuilder.CreateIndex(
				name: "ux_party_bookings_room_date_slot",
				table: "party_bookings",
				columns: new[] { "assigned_room_id", "party_date_utc", "slot_id" },
				unique: true,
				filter: "assigned_room_id IS NOT NULL AND status <> 2");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropIndex(
				name: "ux_party_bookings_room_date_slot",
				table: "party_bookings");

			migrationBuilder.DropColumn(
				name: "assigned_room_id",
				table: "party_bookings");

			migrationBuilder.DropColumn(
				name: "room_assigned_at_utc",
				table: "party_bookings");

			migrationBuilder.DropColumn(
				name: "room_assignment_operation_id",
				table: "party_bookings");
		}
	}
}
