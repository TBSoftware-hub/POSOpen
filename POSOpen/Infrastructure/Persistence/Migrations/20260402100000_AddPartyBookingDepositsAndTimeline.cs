using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POSOpen.Infrastructure.Persistence;

#nullable disable

namespace POSOpen.Infrastructure.Persistence.Migrations
{
	[DbContext(typeof(PosOpenDbContext))]
	[Migration("20260402100000_AddPartyBookingDepositsAndTimeline")]
	public partial class AddPartyBookingDepositsAndTimeline : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<long>(
				name: "deposit_amount_cents",
				table: "party_bookings",
				type: "INTEGER",
				nullable: true);

			migrationBuilder.AddColumn<string>(
				name: "deposit_currency",
				table: "party_bookings",
				type: "TEXT",
				maxLength: 3,
				nullable: true);

			migrationBuilder.AddColumn<string>(
				name: "deposit_committed_at_utc",
				table: "party_bookings",
				type: "TEXT",
				nullable: true);

			migrationBuilder.AddColumn<int>(
				name: "deposit_commitment_status",
				table: "party_bookings",
				type: "INTEGER",
				nullable: false,
				defaultValue: 0);

			migrationBuilder.AddColumn<Guid>(
				name: "deposit_commitment_operation_id",
				table: "party_bookings",
				type: "TEXT",
				nullable: true);

			migrationBuilder.AddColumn<string>(
				name: "completed_at_utc",
				table: "party_bookings",
				type: "TEXT",
				nullable: true);

			migrationBuilder.CreateIndex(
				name: "ix_party_bookings_status_party_date",
				table: "party_bookings",
				columns: new[] { "status", "party_date_utc" });

			migrationBuilder.CreateIndex(
				name: "ux_party_bookings_deposit_commitment_operation_id",
				table: "party_bookings",
				column: "deposit_commitment_operation_id",
				unique: true,
				filter: "deposit_commitment_operation_id IS NOT NULL");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropIndex(
				name: "ix_party_bookings_status_party_date",
				table: "party_bookings");

			migrationBuilder.DropIndex(
				name: "ux_party_bookings_deposit_commitment_operation_id",
				table: "party_bookings");

			migrationBuilder.DropColumn(
				name: "deposit_amount_cents",
				table: "party_bookings");

			migrationBuilder.DropColumn(
				name: "deposit_currency",
				table: "party_bookings");

			migrationBuilder.DropColumn(
				name: "deposit_committed_at_utc",
				table: "party_bookings");

			migrationBuilder.DropColumn(
				name: "deposit_commitment_status",
				table: "party_bookings");

			migrationBuilder.DropColumn(
				name: "deposit_commitment_operation_id",
				table: "party_bookings");

			migrationBuilder.DropColumn(
				name: "completed_at_utc",
				table: "party_bookings");
		}
	}
}
