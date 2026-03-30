using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSOpen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmissionCheckInRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admission_check_in_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    family_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    operation_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    completion_status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    settlement_status = table.Column<int>(type: "INTEGER", nullable: false),
                    amount_cents = table.Column<long>(type: "INTEGER", nullable: false),
                    currency_code = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    completed_at_utc = table.Column<string>(type: "TEXT", nullable: false),
                    settlement_deferred_at_utc = table.Column<string>(type: "TEXT", nullable: true),
                    confirmation_code = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    receipt_reference = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admission_check_in_records", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_admission_check_in_records_family_completed",
                table: "admission_check_in_records",
                columns: new[] { "family_id", "completed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_check_in_records_operation_id",
                table: "admission_check_in_records",
                column: "operation_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admission_check_in_records");
        }
    }
}
