-- Script de correction des migrations PostgreSQL
-- Base de données: laborcontrol
-- Date: 2025-11-12
--
-- Ce script synchronise les migrations manquantes depuis le 29 octobre
-- et crée uniquement les tables/colonnes qui n'existent pas

BEGIN;

-- ============================================================================
-- ÉTAPE 1: Synchroniser l'historique des migrations
-- ============================================================================

-- Migration 1: SyncPendingModelChanges (HomeContents existe déjà)
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251029033325_SyncPendingModelChanges', '9.0.0')
ON CONFLICT DO NOTHING;

-- Migration 2: AddStockReservationToOrders
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'Orders' AND column_name = 'IsStockReserved'
    ) THEN
        ALTER TABLE "Orders" ADD "IsStockReserved" boolean NOT NULL DEFAULT FALSE;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'Orders' AND column_name = 'PreparedAt'
    ) THEN
        ALTER TABLE "Orders" ADD "PreparedAt" timestamp with time zone;
    END IF;
END $$;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251108095934_AddStockReservationToOrders', '9.0.0')
ON CONFLICT DO NOTHING;

-- Migration 3: AddStockReservationToOrders_v2
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251108101147_AddStockReservationToOrders_v2', '9.0.0')
ON CONFLICT DO NOTHING;

-- Migration 4: AddAIFieldsToAssets
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'Assets' AND column_name = 'IsAIAimeeEnabled'
    ) THEN
        ALTER TABLE "Assets" ADD "IsAIAimeeEnabled" boolean NOT NULL DEFAULT FALSE;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'Assets' AND column_name = 'IsAICyrilleEnabled'
    ) THEN
        ALTER TABLE "Assets" ADD "IsAICyrilleEnabled" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $$;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251108131445_AddAIFieldsToAssets', '9.0.0')
ON CONFLICT DO NOTHING;

-- Migration 5: MakeManufacturerAndModelRequired
DO $$
BEGIN
    -- Mettre à jour les valeurs NULL avant de rendre NOT NULL
    UPDATE "Assets" SET "Model" = '' WHERE "Model" IS NULL;
    UPDATE "Assets" SET "Manufacturer" = '' WHERE "Manufacturer" IS NULL;

    -- Modifier les colonnes pour être NOT NULL
    ALTER TABLE "Assets" ALTER COLUMN "Model" SET NOT NULL;
    ALTER TABLE "Assets" ALTER COLUMN "Model" SET DEFAULT '';
    ALTER TABLE "Assets" ALTER COLUMN "Manufacturer" SET NOT NULL;
    ALTER TABLE "Assets" ALTER COLUMN "Manufacturer" SET DEFAULT '';
END $$;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251108132423_MakeManufacturerAndModelRequired', '9.0.0')
ON CONFLICT DO NOTHING;

-- ============================================================================
-- ÉTAPE 2: Créer la table PasswordResetTokens (CRITIQUE)
-- ============================================================================

CREATE TABLE IF NOT EXISTS "PasswordResetTokens" (
    "Id" uuid NOT NULL,
    "UserType" character varying(20) NOT NULL,
    "UserId" uuid NOT NULL,
    "Token" character varying(100) NOT NULL,
    "RequestedFor" character varying(255) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "IsUsed" boolean NOT NULL,
    "UsedAt" timestamp with time zone,
    CONSTRAINT "PK_PasswordResetTokens" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251108195809_AddPasswordResetTokensTable', '9.0.0')
ON CONFLICT DO NOTHING;

-- Migration 7: MakeRfidChipCustomerIdNullable
DO $$
BEGIN
    -- Supprimer la contrainte de clé étrangère si elle existe
    IF EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'FK_RfidChips_Customers_CustomerId'
    ) THEN
        ALTER TABLE "RfidChips" DROP CONSTRAINT "FK_RfidChips_Customers_CustomerId";
    END IF;

    -- Rendre la colonne nullable
    ALTER TABLE "RfidChips" ALTER COLUMN "CustomerId" DROP NOT NULL;

    -- Recréer la contrainte de clé étrangère
    ALTER TABLE "RfidChips" ADD CONSTRAINT "FK_RfidChips_Customers_CustomerId"
        FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id");
END $$;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251110101206_MakeRfidChipCustomerIdNullable', '9.0.0')
ON CONFLICT DO NOTHING;

-- Migration 8: MakeRfidSecurityFieldsNullable
DO $$
BEGIN
    ALTER TABLE "RfidChips" ALTER COLUMN "Salt" DROP NOT NULL;
    ALTER TABLE "RfidChips" ALTER COLUMN "Checksum" DROP NOT NULL;
END $$;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251110151032_MakeRfidSecurityFieldsNullable', '9.0.0')
ON CONFLICT DO NOTHING;

-- ============================================================================
-- ÉTAPE 3: Ajouter les colonnes PasswordReset à StaffUsers (CRITIQUE)
-- ============================================================================

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'StaffUsers' AND column_name = 'PasswordResetToken'
    ) THEN
        ALTER TABLE "StaffUsers" ADD "PasswordResetToken" character varying(500);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'StaffUsers' AND column_name = 'PasswordResetTokenExpiry'
    ) THEN
        ALTER TABLE "StaffUsers" ADD "PasswordResetTokenExpiry" timestamp with time zone;
    END IF;
END $$;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251112074411_AddPasswordResetTokenToStaffUser', '9.0.0')
ON CONFLICT DO NOTHING;

-- ============================================================================
-- VÉRIFICATION FINALE
-- ============================================================================

-- Afficher le statut final
DO $$
DECLARE
    migration_count INTEGER;
    table_exists BOOLEAN;
    column_count INTEGER;
BEGIN
    -- Compter les migrations
    SELECT COUNT(*) INTO migration_count FROM "__EFMigrationsHistory";

    -- Vérifier PasswordResetTokens
    SELECT EXISTS (
        SELECT FROM information_schema.tables WHERE table_name = 'PasswordResetTokens'
    ) INTO table_exists;

    -- Compter les colonnes PasswordReset dans StaffUsers
    SELECT COUNT(*) INTO column_count
    FROM information_schema.columns
    WHERE table_name = 'StaffUsers'
      AND column_name IN ('PasswordResetToken', 'PasswordResetTokenExpiry');

    RAISE NOTICE '========================================';
    RAISE NOTICE 'MIGRATIONS APPLIQUÉES AVEC SUCCÈS';
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Total migrations: %', migration_count;
    RAISE NOTICE 'Table PasswordResetTokens: %', CASE WHEN table_exists THEN 'CRÉÉE' ELSE 'ERREUR' END;
    RAISE NOTICE 'Colonnes StaffUsers: % sur 2', column_count;
    RAISE NOTICE '========================================';
END $$;

COMMIT;

-- Afficher les 5 dernières migrations
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
ORDER BY "MigrationId" DESC
LIMIT 5;
