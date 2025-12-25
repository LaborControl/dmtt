using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class AddZoneIdToControlPoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ZoneId",
                table: "ControlPoints",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ControlPoints_ZoneId",
                table: "ControlPoints",
                column: "ZoneId");

            migrationBuilder.AddForeignKey(
                name: "FK_ControlPoints_Zones_ZoneId",
                table: "ControlPoints",
                column: "ZoneId",
                principalTable: "Zones",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ControlPoints_Zones_ZoneId",
                table: "ControlPoints");

            migrationBuilder.DropIndex(
                name: "IX_ControlPoints_ZoneId",
                table: "ControlPoints");

            migrationBuilder.DropColumn(
                name: "ZoneId",
                table: "ControlPoints");
        }
    }
}
