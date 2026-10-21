using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace enx_fit.Migrations
{
    /// <inheritdoc />
    public partial class CoachCalendarSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlannedMinutes",
                table: "WorkoutSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "ScheduledTime",
                table: "WorkoutSessions",
                type: "time without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlannedMinutes",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "ScheduledTime",
                table: "WorkoutSessions");
        }
    }
}
