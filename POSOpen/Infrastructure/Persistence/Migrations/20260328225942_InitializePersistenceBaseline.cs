using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSOpen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitializePersistenceBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OperationLogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    AggregateId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    OperationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CausationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    OccurredUtc = table.Column<string>(type: "TEXT", nullable: false),
                    RecordedUtc = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationLogEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MessageId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    AggregateId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    OperationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CausationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    OccurredUtc = table.Column<string>(type: "TEXT", nullable: false),
                    EnqueuedUtc = table.Column<string>(type: "TEXT", nullable: false),
                    PublishedUtc = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogEntries_AggregateId_RecordedUtc",
                table: "OperationLogEntries",
                columns: new[] { "AggregateId", "RecordedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogEntries_EventId",
                table: "OperationLogEntries",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_MessageId",
                table: "OutboxMessages",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_PublishedUtc_EnqueuedUtc",
                table: "OutboxMessages",
                columns: new[] { "PublishedUtc", "EnqueuedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperationLogEntries");

            migrationBuilder.DropTable(
                name: "OutboxMessages");
        }
    }
}
