START TRANSACTION;
UPDATE "Users" SET "PasswordHash" = '' WHERE "PasswordHash" IS NULL;
ALTER TABLE "Users" ALTER COLUMN "PasswordHash" SET NOT NULL;
ALTER TABLE "Users" ALTER COLUMN "PasswordHash" SET DEFAULT '';

ALTER TABLE "Users" ADD "SetupPin" character varying(4);

ALTER TABLE "Users" ADD "SetupPinExpiresAt" timestamp with time zone;

ALTER TABLE "HomeContents" ALTER COLUMN "UpdatedAt" SET DEFAULT TIMESTAMPTZ '2025-11-13T05:41:28.056491Z';

ALTER TABLE "HomeContents" ALTER COLUMN "CreatedAt" SET DEFAULT TIMESTAMPTZ '2025-11-13T05:41:28.056407Z';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251113054128_AddSetupPinToUser', '9.0.0');

ALTER TABLE "HomeContents" ALTER COLUMN "UpdatedAt" SET DEFAULT TIMESTAMPTZ '2025-11-13T07:18:40.051965Z';

ALTER TABLE "HomeContents" ALTER COLUMN "CreatedAt" SET DEFAULT TIMESTAMPTZ '2025-11-13T07:18:40.051881Z';

CREATE TABLE "PredefinedSectors" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Code" character varying(50),
    "Description" character varying(500),
    "Color" character varying(20),
    "Icon" character varying(50),
    "DisplayOrder" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_PredefinedSectors" PRIMARY KEY ("Id")
);

CREATE TABLE "PredefinedIndustries" (
    "Id" uuid NOT NULL,
    "PredefinedSectorId" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Code" character varying(50),
    "Description" character varying(500),
    "Color" character varying(20),
    "Icon" character varying(50),
    "DisplayOrder" integer NOT NULL,
    "RecommendedQualifications" text,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_PredefinedIndustries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PredefinedIndustries_PredefinedSectors_PredefinedSectorId" FOREIGN KEY ("PredefinedSectorId") REFERENCES "PredefinedSectors" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_PredefinedIndustries_PredefinedSectorId" ON "PredefinedIndustries" ("PredefinedSectorId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251113071841_AddPredefinedSectorsAndIndustriesTables', '9.0.0');

ALTER TABLE "HomeContents" ALTER COLUMN "UpdatedAt" SET DEFAULT TIMESTAMPTZ '2025-11-13T08:39:03.339506Z';

ALTER TABLE "HomeContents" ALTER COLUMN "CreatedAt" SET DEFAULT TIMESTAMPTZ '2025-11-13T08:39:03.339459Z';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251113083904_UpdatePredefinedTablesConfiguration', '9.0.0');

COMMIT;

