-- Script SQL pour vérifier que la migration Azure a été appliquée correctement
-- À exécuter dans Azure Data Studio ou pgAdmin connecté à Azure PostgreSQL

-- 1. Vérifier que les colonnes Siret, VatNumber, TaxId sont NULLABLE
SELECT
    column_name,
    is_nullable,
    data_type,
    character_maximum_length
FROM information_schema.columns
WHERE table_name = 'Suppliers'
    AND column_name IN ('Siret', 'VatNumber', 'TaxId')
ORDER BY column_name;

-- Résultat attendu :
-- Siret     | YES | character varying | 14
-- TaxId     | YES | character varying | 50
-- VatNumber | YES | character varying | 20

-- 2. Vérifier les index UNIQUE avec filtre (partiels)
SELECT
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'Suppliers'
    AND (indexname LIKE '%Siret%' OR indexname LIKE '%VatNumber%')
ORDER BY indexname;

-- Résultat attendu :
-- IX_Suppliers_Siret     | CREATE UNIQUE INDEX ... WHERE ("Siret" IS NOT NULL)
-- IX_Suppliers_VatNumber | CREATE UNIQUE INDEX ... WHERE ("VatNumber" IS NOT NULL)

-- 3. Vérifier la dernière migration appliquée
SELECT * FROM "__EFMigrationsHistory"
ORDER BY "MigrationId" DESC
LIMIT 5;

-- Résultat attendu : La migration '20251025192058_MakeSupplierIdentifiersNullable' doit apparaître

-- 4. Test d'insertion d'un fournisseur sans SIRET (doit réussir)
-- NE PAS EXÉCUTER EN PRODUCTION - Juste pour vérification
/*
INSERT INTO "Suppliers" (
    "Id",
    "Name",
    "Email",
    "Country",
    "ContactName",
    "Phone",
    "IsActive",
    "PaymentTerms",
    "LeadTimeDays",
    "CreatedAt",
    "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    'Test Supplier International',
    'test@example.com',
    'US',
    'John Doe',
    '+1234567890',
    true,
    '30 days',
    7,
    NOW(),
    NOW()
);

-- Si cette insertion réussit, la migration est correctement appliquée
-- Supprimer ensuite ce test :
DELETE FROM "Suppliers" WHERE "Name" = 'Test Supplier International';
*/
