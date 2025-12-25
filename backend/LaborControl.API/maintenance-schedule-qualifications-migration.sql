START TRANSACTION;
ALTER TABLE "HomeContents" ALTER COLUMN "UpdatedAt" SET DEFAULT TIMESTAMPTZ '2025-11-19T13:51:48.995604Z';

ALTER TABLE "HomeContents" ALTER COLUMN "CreatedAt" SET DEFAULT TIMESTAMPTZ '2025-11-19T13:51:48.995553Z';

CREATE TABLE "MaintenanceScheduleQualifications" (
    "Id" uuid NOT NULL,
    "MaintenanceScheduleId" uuid NOT NULL,
    "QualificationId" uuid NOT NULL,
    "IsMandatory" boolean NOT NULL,
    "AlertLevel" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_MaintenanceScheduleQualifications" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_MaintenanceScheduleQualifications_MaintenanceSchedules_Main~" FOREIGN KEY ("MaintenanceScheduleId") REFERENCES "MaintenanceSchedules" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_MaintenanceScheduleQualifications_Qualifications_Qualificat~" FOREIGN KEY ("QualificationId") REFERENCES "Qualifications" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_MaintenanceScheduleQualifications_MaintenanceScheduleId" ON "MaintenanceScheduleQualifications" ("MaintenanceScheduleId");

CREATE INDEX "IX_MaintenanceScheduleQualifications_QualificationId" ON "MaintenanceScheduleQualifications" ("QualificationId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251119135149_AddMaintenanceScheduleQualifications', '9.0.0');

COMMIT;

