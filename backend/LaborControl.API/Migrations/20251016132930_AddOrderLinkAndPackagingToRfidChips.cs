using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderLinkAndPackagingToRfidChips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "RfidChips",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackagingCode",
                table: "RfidChips",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedDate",
                table: "RfidChips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackagingCode",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RfidChips_OrderId",
                table: "RfidChips",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_RfidChips_Orders_OrderId",
                table: "RfidChips",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RfidChips_Orders_OrderId",
                table: "RfidChips");

            migrationBuilder.DropIndex(
                name: "IX_RfidChips_OrderId",
                table: "RfidChips");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "RfidChips");

            migrationBuilder.DropColumn(
                name: "PackagingCode",
                table: "RfidChips");

            migrationBuilder.DropColumn(
                name: "ReceivedDate",
                table: "RfidChips");

            migrationBuilder.DropColumn(
                name: "PackagingCode",
                table: "Orders");
        }
    }
}
