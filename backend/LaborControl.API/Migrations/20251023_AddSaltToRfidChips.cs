using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSaltToRfidChips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Salt",
                table: "RfidChips",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "RfidChips",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 10, 23, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "RfidChips",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 10, 23, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "RfidChips",
                type: "text",
                nullable: true);

            // Créer un index sur Salt pour les recherches rapides
            migrationBuilder.CreateIndex(
                name: "IX_RfidChips_Salt",
                table: "RfidChips",
                column: "Salt",
                unique: true);

            // Créer un index sur Uid pour les recherches rapides
            migrationBuilder.CreateIndex(
                name: "IX_RfidChips_Uid",
                table: "RfidChips",
                column: "Uid",
                unique: true);

            // Créer un index sur CustomerId pour les requêtes multi-tenant
            migrationBuilder.CreateIndex(
                name: "IX_RfidChips_CustomerId_Status",
                table: "RfidChips",
                columns: new[] { "CustomerId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RfidChips_Salt",
                table: "RfidChips");

            migrationBuilder.DropIndex(
                name: "IX_RfidChips_Uid",
                table: "RfidChips");

            migrationBuilder.DropIndex(
                name: "IX_RfidChips_CustomerId_Status",
                table: "RfidChips");

            migrationBuilder.DropColumn(
                name: "Salt",
                table: "RfidChips");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "RfidChips");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "RfidChips");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "RfidChips");
        }
    }
}
