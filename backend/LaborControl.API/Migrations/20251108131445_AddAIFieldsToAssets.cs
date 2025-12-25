using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAIFieldsToAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 8, 13, 14, 45, 49, DateTimeKind.Utc).AddTicks(7740),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 8, 10, 11, 46, 910, DateTimeKind.Utc).AddTicks(482));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 8, 13, 14, 45, 49, DateTimeKind.Utc).AddTicks(7163),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 8, 10, 11, 46, 910, DateTimeKind.Utc).AddTicks(22));

            migrationBuilder.AddColumn<bool>(
                name: "IsAIAimeeEnabled",
                table: "Assets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAICyrilleEnabled",
                table: "Assets",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAIAimeeEnabled",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "IsAICyrilleEnabled",
                table: "Assets");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 8, 10, 11, 46, 910, DateTimeKind.Utc).AddTicks(482),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 8, 13, 14, 45, 49, DateTimeKind.Utc).AddTicks(7740));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 8, 10, 11, 46, 910, DateTimeKind.Utc).AddTicks(22),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 8, 13, 14, 45, 49, DateTimeKind.Utc).AddTicks(7163));
        }
    }
}
