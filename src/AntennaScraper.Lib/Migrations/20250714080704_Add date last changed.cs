using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AntennaScraper.Lib.Migrations
{
    /// <inheritdoc />
    public partial class Adddatelastchanged : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DateLastChanged",
                table: "BaseStations",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateLastChanged",
                table: "Antennas",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateLastChanged",
                table: "BaseStations");

            migrationBuilder.DropColumn(
                name: "DateLastChanged",
                table: "Antennas");
        }
    }
}
