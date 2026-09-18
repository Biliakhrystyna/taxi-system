using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxiSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDualRoleSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDriver",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPassenger",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Перенести існуючі дані зі старого одинарного "Role" у нові
            // незалежні прапорці, перш ніж колонку буде видалено.
            migrationBuilder.Sql(
                "UPDATE \"Users\" SET \"IsDriver\" = (\"Role\" = 'driver'), \"IsPassenger\" = (\"Role\" = 'passenger');");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Дублі ролі при відкаті втрачаються навмисно: обираємо driver,
            // якщо ввімкнено, інакше passenger — стара схема мала лише одну роль.
            migrationBuilder.Sql(
                "UPDATE \"Users\" SET \"Role\" = CASE WHEN \"IsDriver\" THEN 'driver' ELSE 'passenger' END;");

            migrationBuilder.DropColumn(
                name: "IsDriver",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsPassenger",
                table: "Users");
        }
    }
}
