using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSOpen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCartTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cart_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    family_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    staff_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    cart_status = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at_utc = table.Column<string>(type: "TEXT", nullable: false),
                    updated_at_utc = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cart_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cart_line_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    cart_session_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    fulfillment_context = table.Column<int>(type: "INTEGER", nullable: false),
                    reference_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    unit_amount_cents = table.Column<long>(type: "INTEGER", nullable: false),
                    currency_code = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    created_at_utc = table.Column<string>(type: "TEXT", nullable: false),
                    updated_at_utc = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cart_line_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_cart_line_items_cart_sessions_cart_session_id",
                        column: x => x.cart_session_id,
                        principalTable: "cart_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cart_line_items_cart_session_id",
                table: "cart_line_items",
                column: "cart_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_cart_sessions_staff_status",
                table: "cart_sessions",
                columns: new[] { "staff_id", "cart_status" });

            migrationBuilder.CreateIndex(
                name: "ux_cart_sessions_staff_open",
                table: "cart_sessions",
                column: "staff_id",
                unique: true,
                filter: "cart_status = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "cart_line_items");
            migrationBuilder.DropTable(name: "cart_sessions");
        }
    }
}
