using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxiSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DestinationLat",
                table: "Orders",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DestinationLng",
                table: "Orders",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PickupLat",
                table: "Orders",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PickupLng",
                table: "Orders",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DestinationLat",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DestinationLng",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PickupLat",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PickupLng",
                table: "Orders");
        }
    }
}
