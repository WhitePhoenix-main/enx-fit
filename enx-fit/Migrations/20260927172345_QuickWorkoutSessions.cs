using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace enx_fit.Migrations
{
    /// <inheritdoc />
    public partial class QuickWorkoutSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceProgramRevision",
                table: "WorkoutSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceProgramWorkoutId",
                table: "WorkoutSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceStructureJson",
                table: "WorkoutSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAtUtc",
                table: "WorkoutSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TemplateDecisionPending",
                table: "WorkoutSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "SetEntries",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_OneActivePerUser",
                table: "WorkoutSessions",
                column: "UserId",
                unique: true,
                filter: "\"StartedAtUtc\" IS NOT NULL AND \"CompletedAtUtc\" IS NULL AND \"UserId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkoutSessions_OneActivePerUser",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "SourceProgramRevision",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "SourceProgramWorkoutId",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "SourceStructureJson",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "StartedAtUtc",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "TemplateDecisionPending",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "SetEntries");
        }
    }
}
