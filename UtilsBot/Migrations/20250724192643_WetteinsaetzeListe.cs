using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UtilsBot.Migrations
{
    /// <inheritdoc />
    public partial class WetteinsaetzeListe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BetPlacements_Bet_BetId",
                table: "BetPlacements");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BetPlacements",
                table: "BetPlacements");

            migrationBuilder.RenameTable(
                name: "BetPlacements",
                newName: "Placements");

            migrationBuilder.RenameIndex(
                name: "IX_BetPlacements_BetId",
                table: "Placements",
                newName: "IX_Placements_BetId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Placements",
                table: "Placements",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Placements_Bet_BetId",
                table: "Placements",
                column: "BetId",
                principalTable: "Bet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Placements_Bet_BetId",
                table: "Placements");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Placements",
                table: "Placements");

            migrationBuilder.RenameTable(
                name: "Placements",
                newName: "BetPlacements");

            migrationBuilder.RenameIndex(
                name: "IX_Placements_BetId",
                table: "BetPlacements",
                newName: "IX_BetPlacements_BetId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BetPlacements",
                table: "BetPlacements",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BetPlacements_Bet_BetId",
                table: "BetPlacements",
                column: "BetId",
                principalTable: "Bet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
