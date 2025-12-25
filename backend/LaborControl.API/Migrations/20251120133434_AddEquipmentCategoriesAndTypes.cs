using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentCategoriesAndTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 20, 13, 34, 33, 423, DateTimeKind.Utc).AddTicks(2634),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 19, 13, 51, 48, 995, DateTimeKind.Utc).AddTicks(6041));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 20, 13, 34, 33, 423, DateTimeKind.Utc).AddTicks(2081),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 19, 13, 51, 48, 995, DateTimeKind.Utc).AddTicks(5536));

            migrationBuilder.CreateTable(
                name: "EquipmentCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Icon = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPredefined = table.Column<bool>(type: "boolean", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentCategories_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Icon = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPredefined = table.Column<bool>(type: "boolean", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentTypes_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EquipmentTypes_EquipmentCategories_EquipmentCategoryId",
                        column: x => x.EquipmentCategoryId,
                        principalTable: "EquipmentCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentCategories_CustomerId",
                table: "EquipmentCategories",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentTypes_CustomerId",
                table: "EquipmentTypes",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentTypes_EquipmentCategoryId",
                table: "EquipmentTypes",
                column: "EquipmentCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EquipmentTypes");

            migrationBuilder.DropTable(
                name: "EquipmentCategories");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 19, 13, 51, 48, 995, DateTimeKind.Utc).AddTicks(6041),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 20, 13, 34, 33, 423, DateTimeKind.Utc).AddTicks(2634));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 19, 13, 51, 48, 995, DateTimeKind.Utc).AddTicks(5536),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 20, 13, 34, 33, 423, DateTimeKind.Utc).AddTicks(2081));
        }
    }
}
