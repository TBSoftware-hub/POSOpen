using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POSOpen.Infrastructure.Persistence;

#nullable disable

namespace POSOpen.Infrastructure.Persistence.Migrations
{
	[DbContext(typeof(PosOpenDbContext))]
	[Migration("20260405120000_AddOutboxQueueMetadata")]
	public partial class AddOutboxQueueMetadata : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<Guid>(
				name: "ActorStaffId",
				table: "OutboxMessages",
				type: "TEXT",
				nullable: false,
				defaultValue: Guid.Empty);

			migrationBuilder.AddColumn<long>(
				name: "queue_sequence",
				table: "OutboxMessages",
				type: "INTEGER",
				nullable: false,
				defaultValue: 0L);

			migrationBuilder.Sql(
				"""
				WITH ordered AS (
					SELECT Id, ROW_NUMBER() OVER (ORDER BY EnqueuedUtc, Id) AS seq
					FROM OutboxMessages
				)
				UPDATE OutboxMessages
				SET queue_sequence = (
					SELECT ordered.seq
					FROM ordered
					WHERE ordered.Id = OutboxMessages.Id
				);
				""");

			migrationBuilder.CreateIndex(
				name: "IX_OutboxMessages_queue_sequence",
				table: "OutboxMessages",
				column: "queue_sequence",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_OutboxMessages_PublishedUtc_queue_sequence_EnqueuedUtc",
				table: "OutboxMessages",
				columns: new[] { "PublishedUtc", "queue_sequence", "EnqueuedUtc" });

			migrationBuilder.DropIndex(
				name: "IX_OutboxMessages_PublishedUtc_EnqueuedUtc",
				table: "OutboxMessages");

			migrationBuilder.CreateTable(
				name: "OutboxQueueSequenceAllocations",
				columns: table => new
				{
					SequenceId = table.Column<long>(type: "INTEGER", nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_OutboxQueueSequenceAllocations", x => x.SequenceId);
				});

			migrationBuilder.Sql("INSERT INTO OutboxQueueSequenceAllocations DEFAULT VALUES;");
			migrationBuilder.Sql("DELETE FROM OutboxQueueSequenceAllocations;");
			migrationBuilder.Sql(
				"UPDATE sqlite_sequence SET seq = (SELECT COALESCE(MAX(queue_sequence), 0) FROM OutboxMessages) WHERE name = 'OutboxQueueSequenceAllocations';");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "OutboxQueueSequenceAllocations");

			migrationBuilder.DropIndex(
				name: "IX_OutboxMessages_queue_sequence",
				table: "OutboxMessages");

			migrationBuilder.DropIndex(
				name: "IX_OutboxMessages_PublishedUtc_queue_sequence_EnqueuedUtc",
				table: "OutboxMessages");

			migrationBuilder.CreateIndex(
				name: "IX_OutboxMessages_PublishedUtc_EnqueuedUtc",
				table: "OutboxMessages",
				columns: new[] { "PublishedUtc", "EnqueuedUtc" });

			migrationBuilder.DropColumn(
				name: "ActorStaffId",
				table: "OutboxMessages");

			migrationBuilder.DropColumn(
				name: "queue_sequence",
				table: "OutboxMessages");
		}
	}
}