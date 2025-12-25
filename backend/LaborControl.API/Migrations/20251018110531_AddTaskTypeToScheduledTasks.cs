using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskTypeToScheduledTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledTasks_ControlPoints_ControlPointId",
                table: "ScheduledTasks");

            migrationBuilder.AlterColumn<Guid>(
                name: "ControlPointId",
                table: "ScheduledTasks",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "AssetId",
                table: "ScheduledTasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MaintenanceExecutionId",
                table: "ScheduledTasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MaintenanceExecutionId2",
                table: "ScheduledTasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MaintenanceScheduleId",
                table: "ScheduledTasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaskType",
                table: "ScheduledTasks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "MaintenanceSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Interval = table.Column<int>(type: "integer", nullable: false),
                    OperatingHoursInterval = table.Column<int>(type: "integer", nullable: true),
                    EstimatedDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    RequiredQualification = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DefaultTeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    NextMaintenanceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastMaintenanceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SpecialInstructions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SpareParts = table.Column<string>(type: "text", nullable: true),
                    RequiredTools = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    ManufacturerData = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceSchedules_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaintenanceSchedules_Teams_DefaultTeamId",
                        column: x => x.DefaultTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MaintenanceSchedules_Users_DefaultUserId",
                        column: x => x.DefaultUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintenanceScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaintenanceType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FirstScanAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SecondScanAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualDuration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    TaskResults = table.Column<string>(type: "text", nullable: false),
                    GeneralObservations = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IssuesFound = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CorrectiveActions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Recommendations = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReplacedParts = table.Column<string>(type: "text", nullable: true),
                    ConsumablesUsed = table.Column<string>(type: "text", nullable: true),
                    Photos = table.Column<string>(type: "text", nullable: true),
                    TechnicianSignature = table.Column<string>(type: "text", nullable: true),
                    ClientSignature = table.Column<string>(type: "text", nullable: true),
                    NextMaintenanceRecommended = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextMaintenancePriority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EquipmentCondition = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    WearPercentage = table.Column<int>(type: "integer", nullable: true),
                    QualityValidated = table.Column<bool>(type: "boolean", nullable: false),
                    QualityValidatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    QualityValidatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FlagSaisieRapide = table.Column<bool>(type: "boolean", nullable: false),
                    FlagSaisieDifferee = table.Column<bool>(type: "boolean", nullable: false),
                    FlagValeurRepetee = table.Column<bool>(type: "boolean", nullable: false),
                    FlagHorsMarge = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceExecutions_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceExecutions_MaintenanceSchedules_MaintenanceSched~",
                        column: x => x.MaintenanceScheduleId,
                        principalTable: "MaintenanceSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceExecutions_ScheduledTasks_ScheduledTaskId",
                        column: x => x.ScheduledTaskId,
                        principalTable: "ScheduledTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MaintenanceExecutions_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MaintenanceExecutions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintenanceScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TaskType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    EstimatedDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Instructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AcceptanceCriteria = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SpecificQualification = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SpecificTools = table.Column<string>(type: "text", nullable: true),
                    SpecificParts = table.Column<string>(type: "text", nullable: true),
                    IsMandatory = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresPhoto = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresMeasurement = table.Column<bool>(type: "boolean", nullable: false),
                    MeasurementUnit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MinValue = table.Column<decimal>(type: "numeric", nullable: true),
                    MaxValue = table.Column<decimal>(type: "numeric", nullable: true),
                    SafetyInstructions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceTasks_MaintenanceSchedules_MaintenanceScheduleId",
                        column: x => x.MaintenanceScheduleId,
                        principalTable: "MaintenanceSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTasks_AssetId",
                table: "ScheduledTasks",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTasks_MaintenanceExecutionId2",
                table: "ScheduledTasks",
                column: "MaintenanceExecutionId2");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTasks_MaintenanceScheduleId",
                table: "ScheduledTasks",
                column: "MaintenanceScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceExecutions_AssetId",
                table: "MaintenanceExecutions",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceExecutions_MaintenanceScheduleId",
                table: "MaintenanceExecutions",
                column: "MaintenanceScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceExecutions_ScheduledTaskId",
                table: "MaintenanceExecutions",
                column: "ScheduledTaskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceExecutions_TeamId",
                table: "MaintenanceExecutions",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceExecutions_UserId",
                table: "MaintenanceExecutions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSchedules_AssetId",
                table: "MaintenanceSchedules",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSchedules_DefaultTeamId",
                table: "MaintenanceSchedules",
                column: "DefaultTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSchedules_DefaultUserId",
                table: "MaintenanceSchedules",
                column: "DefaultUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTasks_MaintenanceScheduleId",
                table: "MaintenanceTasks",
                column: "MaintenanceScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledTasks_Assets_AssetId",
                table: "ScheduledTasks",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledTasks_ControlPoints_ControlPointId",
                table: "ScheduledTasks",
                column: "ControlPointId",
                principalTable: "ControlPoints",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledTasks_MaintenanceExecutions_MaintenanceExecutionId2",
                table: "ScheduledTasks",
                column: "MaintenanceExecutionId2",
                principalTable: "MaintenanceExecutions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledTasks_MaintenanceSchedules_MaintenanceScheduleId",
                table: "ScheduledTasks",
                column: "MaintenanceScheduleId",
                principalTable: "MaintenanceSchedules",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledTasks_Assets_AssetId",
                table: "ScheduledTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledTasks_ControlPoints_ControlPointId",
                table: "ScheduledTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledTasks_MaintenanceExecutions_MaintenanceExecutionId2",
                table: "ScheduledTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledTasks_MaintenanceSchedules_MaintenanceScheduleId",
                table: "ScheduledTasks");

            migrationBuilder.DropTable(
                name: "MaintenanceExecutions");

            migrationBuilder.DropTable(
                name: "MaintenanceTasks");

            migrationBuilder.DropTable(
                name: "MaintenanceSchedules");

            migrationBuilder.DropIndex(
                name: "IX_ScheduledTasks_AssetId",
                table: "ScheduledTasks");

            migrationBuilder.DropIndex(
                name: "IX_ScheduledTasks_MaintenanceExecutionId2",
                table: "ScheduledTasks");

            migrationBuilder.DropIndex(
                name: "IX_ScheduledTasks_MaintenanceScheduleId",
                table: "ScheduledTasks");

            migrationBuilder.DropColumn(
                name: "AssetId",
                table: "ScheduledTasks");

            migrationBuilder.DropColumn(
                name: "MaintenanceExecutionId",
                table: "ScheduledTasks");

            migrationBuilder.DropColumn(
                name: "MaintenanceExecutionId2",
                table: "ScheduledTasks");

            migrationBuilder.DropColumn(
                name: "MaintenanceScheduleId",
                table: "ScheduledTasks");

            migrationBuilder.DropColumn(
                name: "TaskType",
                table: "ScheduledTasks");

            migrationBuilder.AlterColumn<Guid>(
                name: "ControlPointId",
                table: "ScheduledTasks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledTasks_ControlPoints_ControlPointId",
                table: "ScheduledTasks",
                column: "ControlPointId",
                principalTable: "ControlPoints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
