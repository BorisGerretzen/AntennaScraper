using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AntennaScraper.Lib.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Layer",
                table: "BaseStations");

            migrationBuilder.DropColumn(
                name: "Layer",
                table: "Antennas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Layer",
                table: "BaseStations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Layer",
                table: "Antennas",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
