using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxiSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDemandZones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MotivationBonus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Zone",
                table: "Orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MotivationBonus",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Zone",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
