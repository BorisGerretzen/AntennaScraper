using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AntennaScraper.Lib.Migrations
{
    /// <inheritdoc />
    public partial class Morefields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "BaseStations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfCommissioning",
                table: "BaseStations",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSmallCell",
                table: "BaseStations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Municipality",
                table: "BaseStations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "BaseStations",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfCommissioning",
                table: "Antennas",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "BaseStations");

            migrationBuilder.DropColumn(
                name: "DateOfCommissioning",
                table: "BaseStations");

            migrationBuilder.DropColumn(
                name: "IsSmallCell",
                table: "BaseStations");

            migrationBuilder.DropColumn(
                name: "Municipality",
                table: "BaseStations");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "BaseStations");

            migrationBuilder.DropColumn(
                name: "DateOfCommissioning",
                table: "Antennas");
        }
    }
}
