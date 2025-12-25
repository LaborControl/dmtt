using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRfidLifecycleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AssignmentDate",
                table: "RfidChips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClientOrderId",
                table: "RfidChips",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ControlPointId",
                table: "RfidChips",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ControlPointId1",
                table: "RfidChips",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredToClientDate",
                table: "RfidChips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EncodingDate",
                table: "RfidChips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstScanDate",
                table: "RfidChips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastScanDate",
                table: "RfidChips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedFromSupplierDate",
                table: "RfidChips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReplacementChipId",
                table: "RfidChips",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SavReason",
                table: "RfidChips",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SavReturnDate",
                table: "RfidChips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShippedToClientDate",
                table: "RfidChips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierOrderId",
                table: "RfidChips",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RfidChipStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RfidChipId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ToStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RfidChipStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RfidChipStatusHistory_RfidChips_RfidChipId",
                        column: x => x.RfidChipId,
                        principalTable: "RfidChips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RfidChipStatusHistory_Users_ChangedBy",
                        column: x => x.ChangedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RfidChips_ControlPointId1",
                table: "RfidChips",
                column: "ControlPointId1");

            migrationBuilder.CreateIndex(
                name: "IX_RfidChips_ReplacementChipId",
                table: "RfidChips",
                column: "ReplacementChipId");

            migrationBuilder.CreateIndex(
                name: "IX_RfidChipStatusHistory_ChangedBy",
                table: "RfidChipStatusHistory",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RfidChipStatusHistory_RfidChipId",
                table: "RfidChipStatusHistory",
                column: "RfidChipId");

            migrationBuilder.AddForeignKey(
                name: "FK_RfidChips_ControlPoints_ControlPointId1",
                table: "RfidChips",
                column: "ControlPointId1",
                principalTable: "ControlPoints",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RfidChips_RfidChips_ReplacementChipId",
                table: "RfidChips",
                column: "ReplacementChipId",
                principalTable: "RfidChips",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RfidChips_ControlPoints_ControlPointId1",
                table: "RfidChips");

            migrationBuilder.DropForeignKey(
                name: "FK_RfidChips_RfidChips_ReplacementChipId",
                table: "RfidChips");

            migrationBuilder.DropTable(
                name: "RfidChipStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_RfidChips_ControlPointId1",
                table: "RfidChips");

            migrationBuilder.DropIndex(
                name: "IX_RfidChips_ReplacementChipId",
                table: "RfidChips");

            migrationBuilder.DropColumn(
                name: "AssignmentDate",
                table: "RfidChips");

            migrationBuilder.DropColumn(
                name: "ClientOrderId",
                table: "RfidChips");

            migrationBuilder.DropColumn(
                name: "ControlPointId",
                table: "RfidChips");

            migrationBuilder.DropColumn(
                name: "ControlPointId1",
                table: "RfidChips");

            migrationBuilder.DropColumn(
                name: "DeliveredToClientDate",
                table: "RfidChips");

            migrationBuilder.DropColumn(
                name: "EncodingDate",
                table: "RfidChips");

            migrationBuilder.DropColumn(
                name: "FirstScanDate",
                table: "RfidChips");

            migrationBuilder.DropColumn(
                name: "LastScanDate",
                table: "RfidChips");

            migrationBuilder.DropColumn(
                name: "ReceivedFromSupplierDate",
                table: "RfidChips");

            migrationBuilder.DropColumn(
                name: "ReplacementChipId",
                table: "RfidChips");

            migrationBuilder.DropColumn(
                name: "SavReason",
                table: "RfidChips");

            migrationBuilder.DropColumn(
                name: "SavReturnDate",
                table: "RfidChips");

            migrationBuilder.DropColumn(
                name: "ShippedToClientDate",
                table: "RfidChips");

            migrationBuilder.DropColumn(
                name: "SupplierOrderId",
                table: "RfidChips");
        }
    }
}
