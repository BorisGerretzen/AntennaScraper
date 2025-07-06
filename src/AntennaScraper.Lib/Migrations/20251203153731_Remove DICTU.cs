using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AntennaScraper.Lib.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDICTU : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateLastChanged",
                table: "BaseStations");

            migrationBuilder.DropColumn(
                name: "DateOfCommissioning",
                table: "BaseStations");

            migrationBuilder.DropColumn(
                name: "SatCode",
                table: "BaseStations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DateLastChanged",
                table: "BaseStations",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfCommissioning",
                table: "BaseStations",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SatCode",
                table: "BaseStations",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");
        }
    }
}
