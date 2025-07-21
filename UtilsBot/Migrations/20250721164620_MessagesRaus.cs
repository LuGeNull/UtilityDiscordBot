using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UtilsBot.Migrations
{
    /// <inheritdoc />
    public partial class MessagesRaus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BenachrichtigenZeitBis",
                table: "AllgemeinePerson");

            migrationBuilder.DropColumn(
                name: "BenachrichtigenZeitVon",
                table: "AllgemeinePerson");

            migrationBuilder.DropColumn(
                name: "WillBenachrichtigungenBekommen",
                table: "AllgemeinePerson");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BenachrichtigenZeitBis",
                table: "AllgemeinePerson",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "BenachrichtigenZeitVon",
                table: "AllgemeinePerson",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "WillBenachrichtigungenBekommen",
                table: "AllgemeinePerson",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
