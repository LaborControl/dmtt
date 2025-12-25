using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class MakeRfidSecurityFieldsNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Salt and Checksum columns may not exist in fresh database - skip these alterations
            // migrationBuilder.AlterColumn<string>(name: "Salt", table: "RfidChips", ...);
            // migrationBuilder.AlterColumn<string>(name: "Checksum", table: "RfidChips", ...);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to undo
        }
    }
}
