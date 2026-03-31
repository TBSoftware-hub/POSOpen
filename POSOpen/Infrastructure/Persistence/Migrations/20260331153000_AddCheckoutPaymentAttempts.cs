using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSOpen.Infrastructure.Persistence.Migrations
{
	public partial class AddCheckoutPaymentAttempts : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "checkout_payment_attempts",
				columns: table => new
				{
					id = table.Column<Guid>(type: "TEXT", nullable: false),
					cart_session_id = table.Column<Guid>(type: "TEXT", nullable: false),
					amount_cents = table.Column<long>(type: "INTEGER", nullable: false),
					currency_code = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
					authorization_status = table.Column<int>(type: "INTEGER", nullable: false),
					processor_reference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
					diagnostic_code = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
					occurred_at_utc = table.Column<string>(type: "TEXT", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_checkout_payment_attempts", x => x.id);
					table.ForeignKey(
						name: "FK_checkout_payment_attempts_cart_sessions_cart_session_id",
						column: x => x.cart_session_id,
						principalTable: "cart_sessions",
						principalColumn: "id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "ix_checkout_payment_attempts_cart_session_id",
				table: "checkout_payment_attempts",
				column: "cart_session_id");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(name: "checkout_payment_attempts");
		}
	}
}