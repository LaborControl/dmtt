using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSectorAndIndustryToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IndustryId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SectorId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IndustryId",
                table: "Users",
                column: "IndustryId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_SectorId",
                table: "Users",
                column: "SectorId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Industries_IndustryId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Sectors_SectorId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_IndustryId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_SectorId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IndustryId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SectorId",
                table: "Users");
        }
    }
}
