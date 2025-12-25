using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduledTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ControlPointId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScheduledTimeStart = table.Column<TimeSpan>(type: "interval", nullable: false),
                    ScheduledTimeEnd = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Recurrence = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TaskExecutionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledTasks_ControlPoints_ControlPointId",
                        column: x => x.ControlPointId,
                        principalTable: "ControlPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduledTasks_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduledTasks_TaskExecutions_TaskExecutionId",
                        column: x => x.TaskExecutionId,
                        principalTable: "TaskExecutions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ScheduledTasks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTasks_ControlPointId",
                table: "ScheduledTasks",
                column: "ControlPointId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTasks_CustomerId",
                table: "ScheduledTasks",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTasks_ScheduledDate",
                table: "ScheduledTasks",
                column: "ScheduledDate");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTasks_Status",
                table: "ScheduledTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTasks_TaskExecutionId",
                table: "ScheduledTasks",
                column: "TaskExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTasks_UserId",
                table: "ScheduledTasks",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduledTasks");
        }
    }
}
