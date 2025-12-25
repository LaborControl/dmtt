using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPredefinedAndIndustryToTaskTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IndustryId",
                table: "TaskTemplates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPredefined",
                table: "TaskTemplates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_TaskTemplates_IndustryId",
                table: "TaskTemplates",
                column: "IndustryId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskTemplates_Industries_IndustryId",
                table: "TaskTemplates",
                column: "IndustryId",
                principalTable: "Industries",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskTemplates_Industries_IndustryId",
                table: "TaskTemplates");

            migrationBuilder.DropIndex(
                name: "IX_TaskTemplates_IndustryId",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "IndustryId",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "IsPredefined",
                table: "TaskTemplates");
        }
    }
}
