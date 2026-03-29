using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSOpen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "staff_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    first_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    password_hash = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    password_salt = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    role = table.Column<int>(type: "INTEGER", nullable: false),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    failed_login_attempts = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    locked_until_utc = table.Column<string>(type: "TEXT", nullable: true),
                    created_at_utc = table.Column<string>(type: "TEXT", nullable: false),
                    updated_at_utc = table.Column<string>(type: "TEXT", nullable: false),
                    created_by_staff_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_by_staff_id = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_accounts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_staff_accounts_email",
                table: "staff_accounts",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staff_accounts");
        }
    }
}
