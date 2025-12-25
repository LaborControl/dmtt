using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePredefinedTablesConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 13, 8, 39, 3, 339, DateTimeKind.Utc).AddTicks(5062),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 13, 7, 18, 40, 51, DateTimeKind.Utc).AddTicks(9655));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 13, 8, 39, 3, 339, DateTimeKind.Utc).AddTicks(4599),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 13, 7, 18, 40, 51, DateTimeKind.Utc).AddTicks(8815));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 13, 7, 18, 40, 51, DateTimeKind.Utc).AddTicks(9655),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 13, 8, 39, 3, 339, DateTimeKind.Utc).AddTicks(5062));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 13, 7, 18, 40, 51, DateTimeKind.Utc).AddTicks(8815),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 13, 8, 39, 3, 339, DateTimeKind.Utc).AddTicks(4599));
        }
    }
}
