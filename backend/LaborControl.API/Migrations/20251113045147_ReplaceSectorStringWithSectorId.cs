using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceSectorStringWithSectorId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Sector",
                table: "Teams");

            migrationBuilder.AddColumn<Guid>(
                name: "SectorId",
                table: "Teams",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 13, 4, 51, 46, 514, DateTimeKind.Utc).AddTicks(336),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 12, 20, 6, 46, 735, DateTimeKind.Utc).AddTicks(9740));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 13, 4, 51, 46, 513, DateTimeKind.Utc).AddTicks(9231),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 12, 20, 6, 46, 735, DateTimeKind.Utc).AddTicks(8723));

            migrationBuilder.CreateIndex(
                name: "IX_Teams_SectorId",
                table: "Teams",
                column: "SectorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Sectors_SectorId",
                table: "Teams",
                column: "SectorId",
                principalTable: "Sectors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Sectors_SectorId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_SectorId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "SectorId",
                table: "Teams");

            migrationBuilder.AddColumn<string>(
                name: "Sector",
                table: "Teams",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 12, 20, 6, 46, 735, DateTimeKind.Utc).AddTicks(9740),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 13, 4, 51, 46, 514, DateTimeKind.Utc).AddTicks(336));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 12, 20, 6, 46, 735, DateTimeKind.Utc).AddTicks(8723),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 13, 4, 51, 46, 513, DateTimeKind.Utc).AddTicks(9231));
        }
    }
}
