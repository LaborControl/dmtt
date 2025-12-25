using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPredefinedQualifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 16, 9, 0, 55, 533, DateTimeKind.Utc).AddTicks(5617),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 14, 9, 35, 53, 192, DateTimeKind.Utc).AddTicks(9862));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 16, 9, 0, 55, 533, DateTimeKind.Utc).AddTicks(4892),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 14, 9, 35, 53, 192, DateTimeKind.Utc).AddTicks(8433));

            migrationBuilder.CreateTable(
                name: "PredefinedQualifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    RncpCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RsCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    FranceCompetencesUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: true),
                    Certificateur = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DateEnregistrement = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DateFinValidite = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Icon = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredefinedQualifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PredefinedQualificationSectors",
                columns: table => new
                {
                    PredefinedQualificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PredefinedSectorId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredefinedQualificationSectors", x => new { x.PredefinedQualificationId, x.PredefinedSectorId });
                    table.ForeignKey(
                        name: "FK_PredefinedQualificationSectors_PredefinedQualifications_Pre~",
                        column: x => x.PredefinedQualificationId,
                        principalTable: "PredefinedQualifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PredefinedQualificationSectors_PredefinedSectors_Predefined~",
                        column: x => x.PredefinedSectorId,
                        principalTable: "PredefinedSectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PredefinedQualificationSectors_PredefinedSectorId",
                table: "PredefinedQualificationSectors",
                column: "PredefinedSectorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PredefinedQualificationSectors");

            migrationBuilder.DropTable(
                name: "PredefinedQualifications");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 14, 9, 35, 53, 192, DateTimeKind.Utc).AddTicks(9862),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 16, 9, 0, 55, 533, DateTimeKind.Utc).AddTicks(5617));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 14, 9, 35, 53, 192, DateTimeKind.Utc).AddTicks(8433),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 16, 9, 0, 55, 533, DateTimeKind.Utc).AddTicks(4892));
        }
    }
}
