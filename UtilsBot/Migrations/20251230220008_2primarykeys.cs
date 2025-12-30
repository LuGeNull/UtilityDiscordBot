using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UtilsBot.Migrations
{
    /// <inheritdoc />
    public partial class _2primarykeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AllgemeinePerson",
                table: "AllgemeinePerson");

            migrationBuilder.RenameTable(
                name: "AllgemeinePerson",
                newName: "AllgemeinePersonen");

            migrationBuilder.AlterColumn<ulong>(
                name: "UserId",
                table: "AllgemeinePersonen",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(ulong),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AllgemeinePersonen",
                table: "AllgemeinePersonen",
                columns: new[] { "UserId", "GuildId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AllgemeinePersonen",
                table: "AllgemeinePersonen");

            migrationBuilder.RenameTable(
                name: "AllgemeinePersonen",
                newName: "AllgemeinePerson");

            migrationBuilder.AlterColumn<ulong>(
                name: "UserId",
                table: "AllgemeinePerson",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(ulong),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AllgemeinePerson",
                table: "AllgemeinePerson",
                column: "UserId");
        }
    }
}
