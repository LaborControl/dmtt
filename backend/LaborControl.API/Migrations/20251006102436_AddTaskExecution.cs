using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ControlPointId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScannedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FormDataJson = table.Column<string>(type: "text", nullable: false),
                    PhotoUrl = table.Column<string>(type: "text", nullable: true),
                    FlagValeurRepetee = table.Column<bool>(type: "boolean", nullable: false),
                    FlagSaisieRapide = table.Column<bool>(type: "boolean", nullable: false),
                    FlagHorsMarge = table.Column<bool>(type: "boolean", nullable: false),
                    FlagEcartOcr = table.Column<bool>(type: "boolean", nullable: false),
                    FlagSaisieDifferee = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskExecutions_ControlPoints_ControlPointId",
                        column: x => x.ControlPointId,
                        principalTable: "ControlPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskExecutions_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskExecutions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskExecutions_ControlPointId",
                table: "TaskExecutions",
                column: "ControlPointId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskExecutions_CustomerId",
                table: "TaskExecutions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskExecutions_ScannedAt",
                table: "TaskExecutions",
                column: "ScannedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaskExecutions_Status",
                table: "TaskExecutions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TaskExecutions_UserId",
                table: "TaskExecutions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskExecutions");
        }
    }
}
