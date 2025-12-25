using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSectorAndIndustryForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Industries_IndustryId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Sectors_SectorId",
                table: "Users");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 21, 6, 23, 32, 738, DateTimeKind.Utc).AddTicks(9609),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 20, 15, 12, 32, 378, DateTimeKind.Utc).AddTicks(3657));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 21, 6, 23, 32, 738, DateTimeKind.Utc).AddTicks(9046),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 20, 15, 12, 32, 378, DateTimeKind.Utc).AddTicks(3030));

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Industries_IndustryId",
                table: "Users",
                column: "IndustryId",
                principalTable: "Industries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Sectors_SectorId",
                table: "Users",
                column: "SectorId",
                principalTable: "Sectors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Industries_IndustryId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Sectors_SectorId",
                table: "Users");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 20, 15, 12, 32, 378, DateTimeKind.Utc).AddTicks(3657),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 21, 6, 23, 32, 738, DateTimeKind.Utc).AddTicks(9609));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 20, 15, 12, 32, 378, DateTimeKind.Utc).AddTicks(3030),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 21, 6, 23, 32, 738, DateTimeKind.Utc).AddTicks(9046));

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Industries_IndustryId",
                table: "Users",
                column: "IndustryId",
                principalTable: "Industries",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Sectors_SectorId",
                table: "Users",
                column: "SectorId",
                principalTable: "Sectors",
                principalColumn: "Id");
        }
    }
}
