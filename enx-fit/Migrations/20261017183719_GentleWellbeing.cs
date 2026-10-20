using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace enx_fit.Migrations
{
    /// <inheritdoc />
    public partial class GentleWellbeing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Feeling",
                table: "DailyCheckIns",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RestDay",
                table: "DailyCheckIns",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Feeling",
                table: "DailyCheckIns");

            migrationBuilder.DropColumn(
                name: "RestDay",
                table: "DailyCheckIns");
        }
    }
}
