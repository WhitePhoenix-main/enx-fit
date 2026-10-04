using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace enx_fit.Migrations
{
    /// <inheritdoc />
    public partial class TrainingPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AvailableEquipment",
                table: "DashboardSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "PreferencesRevision",
                table: "DashboardSettings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "PreferredDays",
                table: "DashboardSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SetupStatus",
                table: "DashboardSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TrainingGoal",
                table: "DashboardSettings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrainingLocation",
                table: "DashboardSettings",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailableEquipment",
                table: "DashboardSettings");

            migrationBuilder.DropColumn(
                name: "PreferencesRevision",
                table: "DashboardSettings");

            migrationBuilder.DropColumn(
                name: "PreferredDays",
                table: "DashboardSettings");

            migrationBuilder.DropColumn(
                name: "SetupStatus",
                table: "DashboardSettings");

            migrationBuilder.DropColumn(
                name: "TrainingGoal",
                table: "DashboardSettings");

            migrationBuilder.DropColumn(
                name: "TrainingLocation",
                table: "DashboardSettings");
        }
    }
}
