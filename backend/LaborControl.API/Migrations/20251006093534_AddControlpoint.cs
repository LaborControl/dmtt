using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class AddControlpoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RfidChips_Customers_CustomerId",
                table: "RfidChips");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Customers_CustomerId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_RfidChips_Uid",
                table: "RfidChips");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Name",
                table: "Customers");

            migrationBuilder.AlterColumn<string>(
                name: "Uid",
                table: "RfidChips",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateTable(
                name: "ControlPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    RfidChipId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationDescription = table.Column<string>(type: "text", nullable: false),
                    PhotoUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ControlPoints_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ControlPoints_RfidChips_RfidChipId",
                        column: x => x.RfidChipId,
                        principalTable: "RfidChips",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ControlPoints_CustomerId",
                table: "ControlPoints",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlPoints_RfidChipId",
                table: "ControlPoints",
                column: "RfidChipId");

            migrationBuilder.AddForeignKey(
                name: "FK_RfidChips_Customers_CustomerId",
                table: "RfidChips",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Customers_CustomerId",
                table: "Users",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RfidChips_Customers_CustomerId",
                table: "RfidChips");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Customers_CustomerId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "ControlPoints");

            migrationBuilder.AlterColumn<string>(
                name: "Uid",
                table: "RfidChips",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_RfidChips_Uid",
                table: "RfidChips",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Name",
                table: "Customers",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_RfidChips_Customers_CustomerId",
                table: "RfidChips",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Customers_CustomerId",
                table: "Users",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
