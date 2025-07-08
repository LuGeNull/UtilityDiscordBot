using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UtilsBot.Migrations
{
    /// <inheritdoc />
    public partial class Benachrichtigungen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BenachrichtigungEingegangen");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BenachrichtigungEingegangen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AllgemeinePersonUserId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    EingegangenVonDisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    EingegangenVonUserID = table.Column<ulong>(type: "INTEGER", nullable: false),
                    EingegangenZeitpunkt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenachrichtigungEingegangen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BenachrichtigungEingegangen_AllgemeinePerson_AllgemeinePersonUserId",
                        column: x => x.AllgemeinePersonUserId,
                        principalTable: "AllgemeinePerson",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BenachrichtigungEingegangen_AllgemeinePersonUserId",
                table: "BenachrichtigungEingegangen",
                column: "AllgemeinePersonUserId");
        }
    }
}
