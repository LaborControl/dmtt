using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class AddNuclearEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanManagePlanning",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanPerformFinalInspection",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanPerformNDTControls",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanPerformNuclearWelds",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanValidateAsCCPU",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanValidateQualifications",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanValidateQuality",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasNuclearClearance",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "NFCAccessLevel",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "NuclearAccessGrantedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "NuclearAccessGrantedById",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NuclearBadgeNumber",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NuclearClearanceExpiresAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NuclearRole",
                table: "Users",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubcontractorCompany",
                table: "Users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 12, 24, 19, 29, 34, 831, DateTimeKind.Utc).AddTicks(9226),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 21, 6, 23, 32, 738, DateTimeKind.Utc).AddTicks(9609));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 12, 24, 19, 29, 34, 831, DateTimeKind.Utc).AddTicks(8748),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 11, 21, 6, 23, 32, 738, DateTimeKind.Utc).AddTicks(9046));

            migrationBuilder.CreateTable(
                name: "DMOSs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    WeldingProcess = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BaseMaterials = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ThicknessRange = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DiameterRange = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    QualifiedPositions = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FillerMetal = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ShieldingGas = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    WeldingParameters = table.Column<string>(type: "jsonb", nullable: true),
                    ApplicableStandards = table.Column<string>(type: "jsonb", nullable: true),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    GeneratedByAI = table.Column<bool>(type: "boolean", nullable: false),
                    AIModelVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AIPrompt = table.Column<string>(type: "text", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovalComments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DMOSs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DMOSs_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DMOSs_Users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Materials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Grade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Specification = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HeatNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BatchNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Supplier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CertificateNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CertificateFilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReceiptDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: true),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Dimensions = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CCPUValidatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CCPUValidationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CCPUComments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    BlockReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SubcontractorId = table.Column<Guid>(type: "uuid", nullable: true),
                    StorageLocation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Materials_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Materials_Users_CCPUValidatorId",
                        column: x => x.CCPUValidatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Materials_Users_SubcontractorId",
                        column: x => x.SubcontractorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NDTPrograms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequiredControls = table.Column<string>(type: "jsonb", nullable: true),
                    AcceptanceCriteria = table.Column<string>(type: "jsonb", nullable: true),
                    ApplicableStandards = table.Column<string>(type: "jsonb", nullable: true),
                    CDCReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    GeneratedByAI = table.Column<bool>(type: "boolean", nullable: false),
                    AIModelVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AIPrompt = table.Column<string>(type: "text", nullable: true),
                    AIInputData = table.Column<string>(type: "jsonb", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovalComments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SamplingRate = table.Column<int>(type: "integer", nullable: false),
                    ControlSequence = table.Column<string>(type: "jsonb", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NDTPrograms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NDTPrograms_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NDTPrograms_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NDTPrograms_Users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WelderQualifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QualificationType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    QualificationNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    WeldingProcess = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CertificationLevel = table.Column<int>(type: "integer", nullable: true),
                    QualifiedMaterials = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ThicknessRange = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DiameterRange = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    QualifiedPositions = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    QualificationStandard = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CertifyingBody = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextRenewalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CertificateFilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TestCouponReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TestCouponPhotoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    QualificationInspector = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ValidatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ValidationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidationComments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PreValidatedByAI = table.Column<bool>(type: "boolean", nullable: false),
                    AIExtractedData = table.Column<string>(type: "jsonb", nullable: true),
                    AIConfidenceScore = table.Column<decimal>(type: "numeric", nullable: true),
                    AIWarnings = table.Column<string>(type: "jsonb", nullable: true),
                    WeldsCompleted = table.Column<int>(type: "integer", nullable: false),
                    WeldsConform = table.Column<int>(type: "integer", nullable: false),
                    WeldsNonConform = table.Column<int>(type: "integer", nullable: false),
                    ControlsPerformed = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WelderQualifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WelderQualifications_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WelderQualifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WelderQualifications_Users_ValidatedById",
                        column: x => x.ValidatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Welds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Diameter = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Thickness = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Material1 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Material2 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    WeldingProcess = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    JointType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    WeldClass = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    WeldingPosition = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    DMOSId = table.Column<Guid>(type: "uuid", nullable: true),
                    WelderId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlannedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExecutionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CCPUValidatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CCPUValidationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CCPUComments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    BlockReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WeldingParameters = table.Column<string>(type: "jsonb", nullable: true),
                    Photos = table.Column<string>(type: "jsonb", nullable: true),
                    WelderObservations = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FirstScanAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SecondScanAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Welds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Welds_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Welds_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Welds_DMOSs_DMOSId",
                        column: x => x.DMOSId,
                        principalTable: "DMOSs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Welds_Users_CCPUValidatorId",
                        column: x => x.CCPUValidatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Welds_Users_WelderId",
                        column: x => x.WelderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NonConformities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    WeldId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    RootCause = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    DetectionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CorrectiveAction = table.Column<string>(type: "text", nullable: true),
                    PreventiveAction = table.Column<string>(type: "text", nullable: true),
                    ActionResponsibleId = table.Column<Guid>(type: "uuid", nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolutionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ClosedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosureComments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Attachments = table.Column<string>(type: "jsonb", nullable: true),
                    ActionHistory = table.Column<string>(type: "jsonb", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "numeric", nullable: true),
                    ScheduleImpactDays = table.Column<int>(type: "integer", nullable: true),
                    RequiresRecontrol = table.Column<bool>(type: "boolean", nullable: false),
                    VerificationControlId = table.Column<Guid>(type: "uuid", nullable: true),
                    AIRecommendation = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonConformities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NonConformities_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NonConformities_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NonConformities_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NonConformities_Users_ActionResponsibleId",
                        column: x => x.ActionResponsibleId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NonConformities_Users_ClosedById",
                        column: x => x.ClosedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NonConformities_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NonConformities_Welds_WeldId",
                        column: x => x.WeldId,
                        principalTable: "Welds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TechnicalDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RevisionIndex = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    WeldId = table.Column<Guid>(type: "uuid", nullable: true),
                    UploadedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Issuer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Tags = table.Column<string>(type: "jsonb", nullable: true),
                    AIExtractedMetadata = table.Column<string>(type: "jsonb", nullable: true),
                    ExtractedText = table.Column<string>(type: "text", nullable: true),
                    AnalyzedByAI = table.Column<bool>(type: "boolean", nullable: false),
                    IsConfidential = table.Column<bool>(type: "boolean", nullable: false),
                    AccessRoles = table.Column<string>(type: "jsonb", nullable: true),
                    DownloadCount = table.Column<int>(type: "integer", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicalDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnicalDocuments_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TechnicalDocuments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TechnicalDocuments_Users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TechnicalDocuments_Users_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TechnicalDocuments_Welds_WeldId",
                        column: x => x.WeldId,
                        principalTable: "Welds",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NDTControls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeldId = table.Column<Guid>(type: "uuid", nullable: false),
                    NDTProgramId = table.Column<Guid>(type: "uuid", nullable: true),
                    ControlType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ControllerId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlannedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ControlDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Result = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ControllerLevel = table.Column<int>(type: "integer", nullable: true),
                    AppliedStandard = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AcceptanceCriteria = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Comments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DefectsFound = table.Column<string>(type: "jsonb", nullable: true),
                    ControlParameters = table.Column<string>(type: "jsonb", nullable: true),
                    Photos = table.Column<string>(type: "jsonb", nullable: true),
                    ReportFilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EnvironmentalConditions = table.Column<string>(type: "jsonb", nullable: true),
                    EquipmentUsed = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EquipmentCalibrationNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FirstScanAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SecondScanAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NonConformityId = table.Column<Guid>(type: "uuid", nullable: true),
                    ControllerSignature = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NDTControls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NDTControls_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NDTControls_NDTPrograms_NDTProgramId",
                        column: x => x.NDTProgramId,
                        principalTable: "NDTPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NDTControls_NonConformities_NonConformityId",
                        column: x => x.NonConformityId,
                        principalTable: "NonConformities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NDTControls_Users_ControllerId",
                        column: x => x.ControllerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NDTControls_Welds_WeldId",
                        column: x => x.WeldId,
                        principalTable: "Welds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DMOSs_ApprovedById",
                table: "DMOSs",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_DMOSs_CustomerId_Reference",
                table: "DMOSs",
                columns: new[] { "CustomerId", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Materials_CCPUValidatorId",
                table: "Materials",
                column: "CCPUValidatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_CustomerId_Reference",
                table: "Materials",
                columns: new[] { "CustomerId", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Materials_SubcontractorId",
                table: "Materials",
                column: "SubcontractorId");

            migrationBuilder.CreateIndex(
                name: "IX_NDTControls_ControllerId",
                table: "NDTControls",
                column: "ControllerId");

            migrationBuilder.CreateIndex(
                name: "IX_NDTControls_CustomerId",
                table: "NDTControls",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_NDTControls_NDTProgramId",
                table: "NDTControls",
                column: "NDTProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_NDTControls_NonConformityId",
                table: "NDTControls",
                column: "NonConformityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NDTControls_WeldId",
                table: "NDTControls",
                column: "WeldId");

            migrationBuilder.CreateIndex(
                name: "IX_NDTPrograms_ApprovedById",
                table: "NDTPrograms",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_NDTPrograms_AssetId",
                table: "NDTPrograms",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_NDTPrograms_CustomerId_Reference",
                table: "NDTPrograms",
                columns: new[] { "CustomerId", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NonConformities_ActionResponsibleId",
                table: "NonConformities",
                column: "ActionResponsibleId");

            migrationBuilder.CreateIndex(
                name: "IX_NonConformities_AssetId",
                table: "NonConformities",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_NonConformities_ClosedById",
                table: "NonConformities",
                column: "ClosedById");

            migrationBuilder.CreateIndex(
                name: "IX_NonConformities_CreatedById",
                table: "NonConformities",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_NonConformities_CustomerId_Reference",
                table: "NonConformities",
                columns: new[] { "CustomerId", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NonConformities_MaterialId",
                table: "NonConformities",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_NonConformities_WeldId",
                table: "NonConformities",
                column: "WeldId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalDocuments_ApprovedById",
                table: "TechnicalDocuments",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalDocuments_AssetId",
                table: "TechnicalDocuments",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalDocuments_CustomerId_Reference_Version",
                table: "TechnicalDocuments",
                columns: new[] { "CustomerId", "Reference", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalDocuments_UploadedById",
                table: "TechnicalDocuments",
                column: "UploadedById");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalDocuments_WeldId",
                table: "TechnicalDocuments",
                column: "WeldId");

            migrationBuilder.CreateIndex(
                name: "IX_WelderQualifications_CustomerId",
                table: "WelderQualifications",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_WelderQualifications_UserId_QualificationType_Qualification~",
                table: "WelderQualifications",
                columns: new[] { "UserId", "QualificationType", "QualificationNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WelderQualifications_ValidatedById",
                table: "WelderQualifications",
                column: "ValidatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Welds_AssetId",
                table: "Welds",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Welds_CCPUValidatorId",
                table: "Welds",
                column: "CCPUValidatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Welds_CustomerId_Reference",
                table: "Welds",
                columns: new[] { "CustomerId", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Welds_DMOSId",
                table: "Welds",
                column: "DMOSId");

            migrationBuilder.CreateIndex(
                name: "IX_Welds_WelderId",
                table: "Welds",
                column: "WelderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NDTControls");

            migrationBuilder.DropTable(
                name: "TechnicalDocuments");

            migrationBuilder.DropTable(
                name: "WelderQualifications");

            migrationBuilder.DropTable(
                name: "NDTPrograms");

            migrationBuilder.DropTable(
                name: "NonConformities");

            migrationBuilder.DropTable(
                name: "Materials");

            migrationBuilder.DropTable(
                name: "Welds");

            migrationBuilder.DropTable(
                name: "DMOSs");

            migrationBuilder.DropColumn(
                name: "CanManagePlanning",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CanPerformFinalInspection",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CanPerformNDTControls",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CanPerformNuclearWelds",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CanValidateAsCCPU",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CanValidateQualifications",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CanValidateQuality",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HasNuclearClearance",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NFCAccessLevel",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NuclearAccessGrantedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NuclearAccessGrantedById",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NuclearBadgeNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NuclearClearanceExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NuclearRole",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SubcontractorCompany",
                table: "Users");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 21, 6, 23, 32, 738, DateTimeKind.Utc).AddTicks(9609),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 12, 24, 19, 29, 34, 831, DateTimeKind.Utc).AddTicks(9226));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "HomeContents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 21, 6, 23, 32, 738, DateTimeKind.Utc).AddTicks(9046),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 12, 24, 19, 29, 34, 831, DateTimeKind.Utc).AddTicks(8748));
        }
    }
}
