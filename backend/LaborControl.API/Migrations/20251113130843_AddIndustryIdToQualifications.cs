using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class AddIndustryIdToQualifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IndustryId",
                table: "Qualifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 13, 13, 8, 42, 178, DateTimeKind.Utc).AddTicks(5377),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 13, 8, 39, 3, 339, DateTimeKind.Utc).AddTicks(5062));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 13, 13, 8, 42, 178, DateTimeKind.Utc).AddTicks(4247),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 13, 8, 39, 3, 339, DateTimeKind.Utc).AddTicks(4599));

            migrationBuilder.CreateIndex(
                name: "IX_Qualifications_IndustryId",
                table: "Qualifications",
                column: "IndustryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Qualifications_Industries_IndustryId",
                table: "Qualifications",
                column: "IndustryId",
                principalTable: "Industries",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Qualifications_Industries_IndustryId",
                table: "Qualifications");

            migrationBuilder.DropIndex(
                name: "IX_Qualifications_IndustryId",
                table: "Qualifications");

            migrationBuilder.DropColumn(
                name: "IndustryId",
                table: "Qualifications");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 13, 8, 39, 3, 339, DateTimeKind.Utc).AddTicks(5062),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 13, 13, 8, 42, 178, DateTimeKind.Utc).AddTicks(5377));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 13, 8, 39, 3, 339, DateTimeKind.Utc).AddTicks(4599),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 13, 13, 8, 42, 178, DateTimeKind.Utc).AddTicks(4247));
        }
    }
}
