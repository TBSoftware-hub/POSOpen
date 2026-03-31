using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSOpen.Infrastructure.Persistence.Migrations
{
	public partial class AddReceiptMetadataAndOperations : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "receipt_metadata",
				columns: table => new
				{
					id = table.Column<Guid>(type: "TEXT", nullable: false),
					operation_id = table.Column<Guid>(type: "TEXT", nullable: false),
					transaction_id = table.Column<Guid>(type: "TEXT", nullable: false),
					amount_cents = table.Column<long>(type: "INTEGER", nullable: false),
					currency_code = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
					item_count = table.Column<int>(type: "INTEGER", nullable: false),
					printed_at_utc = table.Column<string>(type: "TEXT", nullable: true),
					print_status = table.Column<int>(type: "INTEGER", nullable: false),
					diagnostic_code = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
					created_at_utc = table.Column<string>(type: "TEXT", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_receipt_metadata", x => x.id);
				});

			migrationBuilder.CreateIndex(
				name: "ix_receipt_metadata_operation_id",
				table: "receipt_metadata",
				column: "operation_id",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "ix_receipt_metadata_transaction_id",
				table: "receipt_metadata",
				column: "transaction_id");

			migrationBuilder.CreateTable(
				name: "transaction_operations",
				columns: table => new
				{
					id = table.Column<Guid>(type: "TEXT", nullable: false),
					operation_id = table.Column<Guid>(type: "TEXT", nullable: false),
					transaction_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
					operation_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
					operation_data = table.Column<string>(type: "TEXT", nullable: true),
					status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
					created_at_utc = table.Column<string>(type: "TEXT", nullable: false),
					completed_at_utc = table.Column<string>(type: "TEXT", nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_transaction_operations", x => x.id);
				});

			migrationBuilder.CreateIndex(
				name: "ix_transaction_operations_operation_id",
				table: "transaction_operations",
				column: "operation_id",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "ix_transaction_operations_transaction_id",
				table: "transaction_operations",
				column: "transaction_id");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(name: "receipt_metadata");
			migrationBuilder.DropTable(name: "transaction_operations");
		}
	}
}
