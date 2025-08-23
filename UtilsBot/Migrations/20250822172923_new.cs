using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UtilsBot.Migrations
{
    /// <inheritdoc />
    public partial class @new : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ZuletztImChannel",
                table: "AllgemeinePerson",
                newName: "LastTimeInChannel");

            migrationBuilder.RenameColumn(
                name: "BekommtZurzeitSoVielXp",
                table: "AllgemeinePerson",
                newName: "GetsSoMuchXpRightNow");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastTimeInChannel",
                table: "AllgemeinePerson",
                newName: "ZuletztImChannel");

            migrationBuilder.RenameColumn(
                name: "GetsSoMuchXpRightNow",
                table: "AllgemeinePerson",
                newName: "BekommtZurzeitSoVielXp");
        }
    }
}
