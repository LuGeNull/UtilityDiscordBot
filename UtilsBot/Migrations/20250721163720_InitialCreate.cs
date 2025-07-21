using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UtilsBot.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AllgemeinePerson",
                columns: table => new
                {
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    WillBenachrichtigungenBekommen = table.Column<bool>(type: "INTEGER", nullable: false),
                    Xp = table.Column<long>(type: "INTEGER", nullable: false),
                    BekommtZurzeitSoVielXp = table.Column<long>(type: "INTEGER", nullable: false),
                    ZuletztImChannel = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BenachrichtigenZeitVon = table.Column<long>(type: "INTEGER", nullable: false),
                    BenachrichtigenZeitBis = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllgemeinePerson", x => x.UserId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllgemeinePerson");
        }
    }
}
