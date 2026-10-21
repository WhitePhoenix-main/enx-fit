using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace enx_fit.Migrations
{
    /// <inheritdoc />
    public partial class DropSetsAndBodyweight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBodyweight",
                table: "SetEntries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SetDrops",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric", nullable: false),
                    Reps = table.Column<int>(type: "integer", nullable: false),
                    IsBodyweight = table.Column<bool>(type: "boolean", nullable: false),
                    SetEntryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SetDrops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SetDrops_SetEntries_SetEntryId",
                        column: x => x.SetEntryId,
                        principalTable: "SetEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SetDrops_SetEntryId",
                table: "SetDrops",
                column: "SetEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SetDrops");

            migrationBuilder.DropColumn(
                name: "IsBodyweight",
                table: "SetEntries");
        }
    }
}
