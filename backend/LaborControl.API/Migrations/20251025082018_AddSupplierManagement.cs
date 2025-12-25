using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ControlPoints_RfidChips_RfidChipId",
                table: "ControlPoints");

            migrationBuilder.DropForeignKey(
                name: "FK_ControlPoints_Zones_ZoneId",
                table: "ControlPoints");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Sites_SiteId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_SiteId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "Qualification",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "RequiredQualification",
                table: "TaskTemplates");

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContactName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "text", nullable: false),
                    PostalCode = table.Column<string>(type: "text", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: false),
                    Siret = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    Website = table.Column<string>(type: "text", nullable: false),
                    PaymentTerms = table.Column<string>(type: "text", nullable: false),
                    LeadTimeDays = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierOrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ReceivedQuantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierOrderLines_SupplierOrders_SupplierOrderId",
                        column: x => x.SupplierOrderId,
                        principalTable: "SupplierOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RfidChips_SupplierOrderId",
                table: "RfidChips",
                column: "SupplierOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierOrderLines_SupplierOrderId",
                table: "SupplierOrderLines",
                column: "SupplierOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierOrders_OrderNumber",
                table: "SupplierOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierOrders_SupplierId",
                table: "SupplierOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Siret",
                table: "Suppliers",
                column: "Siret",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ControlPoints_RfidChips_RfidChipId",
                table: "ControlPoints",
                column: "RfidChipId",
                principalTable: "RfidChips",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ControlPoints_Zones_ZoneId",
                table: "ControlPoints",
                column: "ZoneId",
                principalTable: "Zones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RfidChips_SupplierOrders_SupplierOrderId",
                table: "RfidChips",
                column: "SupplierOrderId",
                principalTable: "SupplierOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ControlPoints_RfidChips_RfidChipId",
                table: "ControlPoints");

            migrationBuilder.DropForeignKey(
                name: "FK_ControlPoints_Zones_ZoneId",
                table: "ControlPoints");

            migrationBuilder.DropForeignKey(
                name: "FK_RfidChips_SupplierOrders_SupplierOrderId",
                table: "RfidChips");

            migrationBuilder.DropTable(
                name: "SupplierOrderLines");

            migrationBuilder.DropTable(
                name: "SupplierOrders");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_RfidChips_SupplierOrderId",
                table: "RfidChips");

            migrationBuilder.AddColumn<string>(
                name: "Qualification",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SiteId",
                table: "Teams",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequiredQualification",
                table: "TaskTemplates",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_SiteId",
                table: "Teams",
                column: "SiteId");

            migrationBuilder.AddForeignKey(
                name: "FK_ControlPoints_RfidChips_RfidChipId",
                table: "ControlPoints",
                column: "RfidChipId",
                principalTable: "RfidChips",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ControlPoints_Zones_ZoneId",
                table: "ControlPoints",
                column: "ZoneId",
                principalTable: "Zones",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Sites_SiteId",
                table: "Teams",
                column: "SiteId",
                principalTable: "Sites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
