using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UtilsBot.Migrations
{
    /// <inheritdoc />
    public partial class AddXpTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastXpGainDate",
                table: "AllgemeinePerson",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "XpTodayByMessages",
                table: "AllgemeinePerson",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastXpGainDate",
                table: "AllgemeinePerson");

            migrationBuilder.DropColumn(
                name: "XpTodayByMessages",
                table: "AllgemeinePerson");
        }
    }
}
