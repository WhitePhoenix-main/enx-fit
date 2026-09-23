using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace enx_fit.Migrations
{
    /// <inheritdoc />
    public partial class WorkoutBuilderConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BuilderConfigurationJson",
                table: "WorkoutSessions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "BuilderConfigurationJson", table: "WorkoutSessions");
        }
    }
}
