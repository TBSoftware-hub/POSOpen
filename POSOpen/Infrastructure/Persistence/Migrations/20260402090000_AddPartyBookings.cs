using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POSOpen.Infrastructure.Persistence;

#nullable disable

namespace POSOpen.Infrastructure.Persistence.Migrations
{
	/// <inheritdoc />
	[DbContext(typeof(PosOpenDbContext))]
	[Migration("20260402090000_AddPartyBookings")]
	public partial class AddPartyBookings : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "party_bookings",
				columns: table => new
				{
					id = table.Column<Guid>(type: "TEXT", nullable: false),
					party_date_utc = table.Column<string>(type: "TEXT", nullable: false),
					slot_id = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
					package_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
					status = table.Column<int>(type: "INTEGER", nullable: false),
					operation_id = table.Column<Guid>(type: "TEXT", nullable: false),
					correlation_id = table.Column<Guid>(type: "TEXT", nullable: false),
					created_at_utc = table.Column<string>(type: "TEXT", nullable: false),
					updated_at_utc = table.Column<string>(type: "TEXT", nullable: false),
					booked_at_utc = table.Column<string>(type: "TEXT", nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_party_bookings", x => x.id);
				});

			migrationBuilder.CreateIndex(
				name: "ix_party_bookings_operation_id",
				table: "party_bookings",
				column: "operation_id");

			migrationBuilder.CreateIndex(
				name: "ix_party_bookings_party_date_slot_status",
				table: "party_bookings",
				columns: new[] { "party_date_utc", "slot_id", "status" });

			migrationBuilder.CreateIndex(
				name: "ux_party_bookings_active_slot",
				table: "party_bookings",
				columns: new[] { "party_date_utc", "slot_id" },
				unique: true,
				filter: "status <> 2");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(name: "party_bookings");
		}
	}
}
