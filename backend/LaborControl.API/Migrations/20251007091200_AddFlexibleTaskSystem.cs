using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFlexibleTaskSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledTasks_TaskExecutions_TaskExecutionId",
                table: "ScheduledTasks");

            migrationBuilder.DropIndex(
                name: "IX_TaskExecutions_ScannedAt",
                table: "TaskExecutions");

            migrationBuilder.DropIndex(
                name: "IX_TaskExecutions_Status",
                table: "TaskExecutions");

            migrationBuilder.DropIndex(
                name: "IX_ScheduledTasks_ScheduledDate",
                table: "ScheduledTasks");

            migrationBuilder.DropIndex(
                name: "IX_ScheduledTasks_Status",
                table: "ScheduledTasks");

            migrationBuilder.DropIndex(
                name: "IX_ScheduledTasks_TaskExecutionId",
                table: "ScheduledTasks");

            migrationBuilder.DropColumn(
                name: "FormDataJson",
                table: "TaskExecutions");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "TaskExecutions");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "TaskExecutions",
                newName: "FormData");

            migrationBuilder.AddColumn<bool>(
                name: "CanApproveDeviations",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Qualification",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmittedAt",
                table: "TaskExecutions",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<bool>(
                name: "FlagOutOfRange",
                table: "TaskExecutions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FlagQuickEntry",
                table: "TaskExecutions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FlagSuspiciousValue",
                table: "TaskExecutions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ScheduledTaskId",
                table: "TaskExecutions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TaskExecutionId1",
                table: "ScheduledTasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TaskTemplateId",
                table: "ScheduledTasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TaskDeviations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpectedQualification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ActualQualification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WasApproved = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    JustificationComment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsReported = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskDeviations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskDeviations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskDeviations_TaskExecutions_TaskExecutionId",
                        column: x => x.TaskExecutionId,
                        principalTable: "TaskExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskDeviations_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskDeviations_Users_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequiredQualification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsUniversal = table.Column<bool>(type: "boolean", nullable: false),
                    AlertOnMismatch = table.Column<bool>(type: "boolean", nullable: false),
                    LegalWarning = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FormTemplate = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskTemplates_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskExecutions_ScheduledTaskId",
                table: "TaskExecutions",
                column: "ScheduledTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTasks_TaskExecutionId1",
                table: "ScheduledTasks",
                column: "TaskExecutionId1");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTasks_TaskTemplateId",
                table: "ScheduledTasks",
                column: "TaskTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskDeviations_ApprovedByUserId",
                table: "TaskDeviations",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskDeviations_CustomerId",
                table: "TaskDeviations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskDeviations_PerformedByUserId",
                table: "TaskDeviations",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskDeviations_TaskExecutionId",
                table: "TaskDeviations",
                column: "TaskExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskTemplates_CustomerId",
                table: "TaskTemplates",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledTasks_TaskExecutions_TaskExecutionId1",
                table: "ScheduledTasks",
                column: "TaskExecutionId1",
                principalTable: "TaskExecutions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledTasks_TaskTemplates_TaskTemplateId",
                table: "ScheduledTasks",
                column: "TaskTemplateId",
                principalTable: "TaskTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskExecutions_ScheduledTasks_ScheduledTaskId",
                table: "TaskExecutions",
                column: "ScheduledTaskId",
                principalTable: "ScheduledTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledTasks_TaskExecutions_TaskExecutionId1",
                table: "ScheduledTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledTasks_TaskTemplates_TaskTemplateId",
                table: "ScheduledTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskExecutions_ScheduledTasks_ScheduledTaskId",
                table: "TaskExecutions");

            migrationBuilder.DropTable(
                name: "TaskDeviations");

            migrationBuilder.DropTable(
                name: "TaskTemplates");

            migrationBuilder.DropIndex(
                name: "IX_TaskExecutions_ScheduledTaskId",
                table: "TaskExecutions");

            migrationBuilder.DropIndex(
                name: "IX_ScheduledTasks_TaskExecutionId1",
                table: "ScheduledTasks");

            migrationBuilder.DropIndex(
                name: "IX_ScheduledTasks_TaskTemplateId",
                table: "ScheduledTasks");

            migrationBuilder.DropColumn(
                name: "CanApproveDeviations",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Qualification",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FlagOutOfRange",
                table: "TaskExecutions");

            migrationBuilder.DropColumn(
                name: "FlagQuickEntry",
                table: "TaskExecutions");

            migrationBuilder.DropColumn(
                name: "FlagSuspiciousValue",
                table: "TaskExecutions");

            migrationBuilder.DropColumn(
                name: "ScheduledTaskId",
                table: "TaskExecutions");

            migrationBuilder.DropColumn(
                name: "TaskExecutionId1",
                table: "ScheduledTasks");

            migrationBuilder.DropColumn(
                name: "TaskTemplateId",
                table: "ScheduledTasks");

            migrationBuilder.RenameColumn(
                name: "FormData",
                table: "TaskExecutions",
                newName: "Status");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmittedAt",
                table: "TaskExecutions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormDataJson",
                table: "TaskExecutions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "TaskExecutions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskExecutions_ScannedAt",
                table: "TaskExecutions",
                column: "ScannedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaskExecutions_Status",
                table: "TaskExecutions",
                column: "Status");

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

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledTasks_TaskExecutions_TaskExecutionId",
                table: "ScheduledTasks",
                column: "TaskExecutionId",
                principalTable: "TaskExecutions",
                principalColumn: "Id");
        }
    }
}
