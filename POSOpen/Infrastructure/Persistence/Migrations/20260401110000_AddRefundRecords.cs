using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSOpen.Infrastructure.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class AddRefundRecords : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "refund_records",
				columns: table => new
				{
					id = table.Column<Guid>(type: "TEXT", nullable: false),
					cart_session_id = table.Column<Guid>(type: "TEXT", nullable: false),
					operation_id = table.Column<Guid>(type: "TEXT", nullable: false),
					status = table.Column<int>(type: "INTEGER", nullable: false),
					path = table.Column<int>(type: "INTEGER", nullable: false),
					amount_cents = table.Column<long>(type: "INTEGER", nullable: false),
					currency_code = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
					reason = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
					actor_staff_id = table.Column<Guid>(type: "TEXT", nullable: false),
					actor_role = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
					created_at_utc = table.Column<string>(type: "TEXT", nullable: false),
					completed_at_utc = table.Column<string>(type: "TEXT", nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_refund_records", x => x.id);
					table.ForeignKey(
						name: "FK_refund_records_cart_sessions_cart_session_id",
						column: x => x.cart_session_id,
						principalTable: "cart_sessions",
						principalColumn: "id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "ix_refund_records_cart_session_id",
				table: "refund_records",
				column: "cart_session_id");

			migrationBuilder.CreateIndex(
				name: "ix_refund_records_operation_id",
				table: "refund_records",
				column: "operation_id",
				unique: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(name: "refund_records");
		}
	}
}