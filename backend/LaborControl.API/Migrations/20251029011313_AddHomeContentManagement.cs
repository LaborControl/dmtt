using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class AddHomeContentManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // StaffUsers already created by previous migration 20251028193157_CreateStaffUsersTable
            // HomeContents table should be created separately if needed
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to undo - StaffUsers handled by 20251028193157_CreateStaffUsersTable
        }
    }
}
