using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace enx_fit.Migrations
{
    /// <inheritdoc />
    public partial class ProgramScheduleChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkoutSessions_TrainingProgramId_ProgramWorkoutKey_Schedul~",
                table: "WorkoutSessions");

            migrationBuilder.CreateTable(
                name: "ProgramScheduleChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingProgramId = table.Column<int>(type: "integer", nullable: false),
                    ProgramWorkoutKey = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramScheduleChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgramScheduleChanges_TrainingPrograms_TrainingProgramId",
                        column: x => x.TrainingProgramId,
                        principalTable: "TrainingPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_TrainingProgramId_ProgramWorkoutKey_Schedul~",
                table: "WorkoutSessions",
                columns: new[] { "TrainingProgramId", "ProgramWorkoutKey", "ScheduledDate" },
                unique: true,
                filter: "\"Status\" <> 5");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleChanges_TrainingProgramId_ProgramWorkoutKey_~",
                table: "ProgramScheduleChanges",
                columns: new[] { "TrainingProgramId", "ProgramWorkoutKey", "OriginalDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProgramScheduleChanges");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSessions_TrainingProgramId_ProgramWorkoutKey_Schedul~",
                table: "WorkoutSessions");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_TrainingProgramId_ProgramWorkoutKey_Schedul~",
                table: "WorkoutSessions",
                columns: new[] { "TrainingProgramId", "ProgramWorkoutKey", "ScheduledDate" },
                unique: true);
        }
    }
}
