using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class AddQualificationPredefinedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Qualifications",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Qualifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "Qualifications",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IndustryId",
                table: "Qualifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPredefined",
                table: "Qualifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

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
                name: "Color",
                table: "Qualifications");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "Qualifications");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "Qualifications");

            migrationBuilder.DropColumn(
                name: "IndustryId",
                table: "Qualifications");

            migrationBuilder.DropColumn(
                name: "IsPredefined",
                table: "Qualifications");
        }
    }
}
