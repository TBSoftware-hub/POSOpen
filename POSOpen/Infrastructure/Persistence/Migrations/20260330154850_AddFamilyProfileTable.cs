using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSOpen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyProfileTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "family_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    first_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: true),
                    waiver_status = table.Column<int>(type: "INTEGER", nullable: false),
                    waiver_completed_at_utc = table.Column<string>(type: "TEXT", nullable: true),
                    scan_token = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    created_at_utc = table.Column<string>(type: "TEXT", nullable: false),
                    updated_at_utc = table.Column<string>(type: "TEXT", nullable: false),
                    created_by_staff_id = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_family_profiles", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_family_profiles_last_name",
                table: "family_profiles",
                column: "last_name");

            migrationBuilder.CreateIndex(
                name: "ix_family_profiles_phone",
                table: "family_profiles",
                column: "phone");

            migrationBuilder.CreateIndex(
                name: "ix_family_profiles_scan_token",
                table: "family_profiles",
                column: "scan_token");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "family_profiles");
        }
    }
}
