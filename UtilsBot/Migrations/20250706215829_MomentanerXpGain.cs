﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UtilsBot.Migrations
{
    /// <inheritdoc />
    public partial class MomentanerXpGain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BekommtZurzeitSoVielXp",
                table: "AllgemeinePerson",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BekommtZurzeitSoVielXp",
                table: "AllgemeinePerson");
        }
    }
}
