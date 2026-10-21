using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace enx_fit.Migrations
{
    /// <inheritdoc />
    public partial class TrainingExperience : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TrainingLevel",
                table: "DashboardSettings",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrainingLevel",
                table: "DashboardSettings");
        }
    }
}
