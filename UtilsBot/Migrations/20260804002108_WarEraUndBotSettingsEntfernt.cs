using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UtilsBot.Migrations
{
    /// <inheritdoc />
    public partial class WarEraUndBotSettingsEntfernt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BotSettings");

            migrationBuilder.DropTable(
                name: "WarEraContractMessages");

            migrationBuilder.DropTable(
                name: "WarEraContracts");

            migrationBuilder.DropTable(
                name: "WarEraSubscriptions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BotSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSettings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "WarEraContractMessages",
                columns: table => new
                {
                    ContractId = table.Column<string>(type: "TEXT", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    MessageId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarEraContractMessages", x => new { x.ContractId, x.ChannelId });
                });

            migrationBuilder.CreateTable(
                name: "WarEraContracts",
                columns: table => new
                {
                    ContractId = table.Column<string>(type: "TEXT", nullable: false),
                    LastPerK = table.Column<decimal>(type: "TEXT", nullable: false),
                    NotifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarEraContracts", x => x.ContractId);
                });

            migrationBuilder.CreateTable(
                name: "WarEraSubscriptions",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ExcludedTargetCountryCodes = table.Column<string>(type: "TEXT", nullable: false),
                    IncludeProContracts = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaximumDamage = table.Column<long>(type: "INTEGER", nullable: false),
                    MinimumRate = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarEraSubscriptions", x => x.GuildId);
                });
        }
    }
}
