using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceIndustryWithSectorInQualifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Qualifications_Industries_IndustryId",
                table: "Qualifications");

            migrationBuilder.RenameColumn(
                name: "IndustryId",
                table: "Qualifications",
                newName: "SectorId");

            migrationBuilder.RenameIndex(
                name: "IX_Qualifications_IndustryId",
                table: "Qualifications",
                newName: "IX_Qualifications_SectorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Qualifications_Sectors_SectorId",
                table: "Qualifications",
                column: "SectorId",
                principalTable: "Sectors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Qualifications_Sectors_SectorId",
                table: "Qualifications");

            migrationBuilder.RenameColumn(
                name: "SectorId",
                table: "Qualifications",
                newName: "IndustryId");

            migrationBuilder.RenameIndex(
                name: "IX_Qualifications_SectorId",
                table: "Qualifications",
                newName: "IX_Qualifications_IndustryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Qualifications_Industries_IndustryId",
                table: "Qualifications",
                column: "IndustryId",
                principalTable: "Industries",
                principalColumn: "Id");
        }
    }
}
