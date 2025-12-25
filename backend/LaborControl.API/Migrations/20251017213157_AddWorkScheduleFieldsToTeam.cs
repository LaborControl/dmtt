using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkScheduleFieldsToTeam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasOnCallDuty",
                table: "Teams",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RotationFrequency",
                table: "Teams",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkShiftSystem",
                table: "Teams",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WorksSaturday",
                table: "Teams",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WorksSunday",
                table: "Teams",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasOnCallDuty",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "RotationFrequency",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "WorkShiftSystem",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "WorksSaturday",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "WorksSunday",
                table: "Teams");
        }
    }
}
