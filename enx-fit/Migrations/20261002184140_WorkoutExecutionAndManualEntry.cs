using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace enx_fit.Migrations
{
    /// <inheritdoc />
    public partial class WorkoutExecutionAndManualEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClientRequestId",
                table: "WorkoutSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationSeconds",
                table: "WorkoutSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EntryMode",
                table: "WorkoutSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ManualPayloadHash",
                table: "WorkoutSessions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PausedAtUtc",
                table: "WorkoutSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PausedSeconds",
                table: "WorkoutSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RestAfterSetId",
                table: "WorkoutSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RestEndsAtUtc",
                table: "WorkoutSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RestRemainingSeconds",
                table: "WorkoutSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Revision",
                table: "WorkoutSessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "WorkoutSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UtcOffsetMinutes",
                table: "WorkoutSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BlockKind",
                table: "WorkoutExercises",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetRestSeconds",
                table: "WorkoutExercises",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSkipped",
                table: "SetEntries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PerformedAtUtc",
                table: "SetEntries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RestSeconds",
                table: "SetEntries",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkoutCommand",
                columns: table => new
                {
                    WorkoutSessionId = table.Column<int>(type: "integer", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutCommand", x => new { x.WorkoutSessionId, x.OperationId });
                    table.ForeignKey(
                        name: "FK_WorkoutCommand_WorkoutSessions_WorkoutSessionId",
                        column: x => x.WorkoutSessionId,
                        principalTable: "WorkoutSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Preserve the interpretation of existing diary entries; do not change their dates/results.
            migrationBuilder.Sql("""
                UPDATE "WorkoutSessions" AS w SET "Status" = CASE
                    WHEN w."CompletedAtUtc" IS NOT NULL THEN 4
                    WHEN w."StartedAtUtc" IS NOT NULL OR w."TrainingProgramId" IS NOT NULL THEN 2
                    WHEN EXISTS (SELECT 1 FROM "WorkoutExercises" AS e JOIN "SetEntries" AS s
                        ON s."WorkoutExerciseId" = e."Id" WHERE e."WorkoutSessionId" = w."Id"
                        AND s."IsCompleted" AND NOT s."IsWarmup" AND s."Reps" > 0) THEN 4
                    ELSE 1 END;
                UPDATE "WorkoutSessions" SET "EntryMode" = 1 WHERE "Status" = 4 AND "StartedAtUtc" IS NULL;
                UPDATE "WorkoutSessions" SET "DurationSeconds" = GREATEST(0, FLOOR(EXTRACT(EPOCH FROM
                    ("CompletedAtUtc" - "StartedAtUtc")))::integer)
                    WHERE "CompletedAtUtc" IS NOT NULL AND "StartedAtUtc" IS NOT NULL;
                """);
            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_UserId_ClientRequestId",
                table: "WorkoutSessions",
                columns: new[] { "UserId", "ClientRequestId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkoutCommand");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSessions_UserId_ClientRequestId",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "ClientRequestId",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "EntryMode",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "ManualPayloadHash",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "PausedAtUtc",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "PausedSeconds",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "RestAfterSetId",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "RestEndsAtUtc",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "RestRemainingSeconds",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "UtcOffsetMinutes",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "BlockKind",
                table: "WorkoutExercises");

            migrationBuilder.DropColumn(
                name: "TargetRestSeconds",
                table: "WorkoutExercises");

            migrationBuilder.DropColumn(
                name: "IsSkipped",
                table: "SetEntries");

            migrationBuilder.DropColumn(
                name: "PerformedAtUtc",
                table: "SetEntries");

            migrationBuilder.DropColumn(
                name: "RestSeconds",
                table: "SetEntries");

        }
    }
}
