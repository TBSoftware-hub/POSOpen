using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSOpen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventorySubstitutionPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_substitution_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    source_option_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    allowed_substitute_option_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    allowed_roles_csv = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at_utc = table.Column<string>(type: "TEXT", nullable: false),
                    updated_at_utc = table.Column<string>(type: "TEXT", nullable: false),
                    created_by_staff_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    updated_by_staff_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    last_operation_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_substitution_policies", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_sub_policies_last_operation_id",
                table: "inventory_substitution_policies",
                column: "last_operation_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_sub_policies_source_substitute",
                table: "inventory_substitution_policies",
                columns: new[] { "source_option_id", "allowed_substitute_option_id" });

            migrationBuilder.CreateIndex(
                name: "ux_inventory_sub_policies_active_combo",
                table: "inventory_substitution_policies",
                columns: new[] { "source_option_id", "allowed_substitute_option_id", "allowed_roles_csv", "is_active" },
                unique: true,
                filter: "is_active = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_substitution_policies");
        }
    }
}
