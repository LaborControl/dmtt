using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceScheduleQualifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 19, 13, 51, 48, 995, DateTimeKind.Utc).AddTicks(6041),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 17, 15, 12, 2, 32, DateTimeKind.Utc).AddTicks(7144));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 19, 13, 51, 48, 995, DateTimeKind.Utc).AddTicks(5536),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 17, 15, 12, 2, 32, DateTimeKind.Utc).AddTicks(6682));

            migrationBuilder.CreateTable(
                name: "MaintenanceScheduleQualifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintenanceScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    QualificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsMandatory = table.Column<bool>(type: "boolean", nullable: false),
                    AlertLevel = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceScheduleQualifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceScheduleQualifications_MaintenanceSchedules_Main~",
                        column: x => x.MaintenanceScheduleId,
                        principalTable: "MaintenanceSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaintenanceScheduleQualifications_Qualifications_Qualificat~",
                        column: x => x.QualificationId,
                        principalTable: "Qualifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceScheduleQualifications_MaintenanceScheduleId",
                table: "MaintenanceScheduleQualifications",
                column: "MaintenanceScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceScheduleQualifications_QualificationId",
                table: "MaintenanceScheduleQualifications",
                column: "QualificationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaintenanceScheduleQualifications");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 17, 15, 12, 2, 32, DateTimeKind.Utc).AddTicks(7144),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 19, 13, 51, 48, 995, DateTimeKind.Utc).AddTicks(6041));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 17, 15, 12, 2, 32, DateTimeKind.Utc).AddTicks(6682),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 19, 13, 51, 48, 995, DateTimeKind.Utc).AddTicks(5536));
        }
    }
}
