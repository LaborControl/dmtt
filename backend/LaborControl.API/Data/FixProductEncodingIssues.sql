-- Script de correction des caractères mal encodés dans la table Products
-- Problème : Caractères UTF-8 mal interprétés (é → ├®, à → ├á, ô → ├┤, € → Ôé¼)
-- Solution : Remplacer les caractères corrompus par les bons

-- 1. Corriger les noms de produits
UPDATE "Products"
SET "Name" =
    REPLACE(
        REPLACE(
            REPLACE(
                REPLACE(
                    REPLACE("Name", '├®', 'é'),
                    '├á', 'à'),
                '├┤', 'ô'),
            'Ôé¼', '€'),
        '├®', 'é')
WHERE "Name" LIKE '%├%' OR "Name" LIKE '%Ôé¼%';

-- 2. Corriger les descriptions de produits
UPDATE "Products"
SET "Description" =
    REPLACE(
        REPLACE(
            REPLACE(
                REPLACE(
                    REPLACE("Description", '├®', 'é'),
                    '├á', 'à'),
                '├┤', 'ô'),
            'Ôé¼', '€'),
        '├®', 'é')
WHERE "Description" LIKE '%├%' OR "Description" LIKE '%Ôé¼%';

-- 3. Vérifier les corrections
SELECT "Id", "Name", "Description" FROM "Products" WHERE "ProductType" IN ('pack_discovery', 'nfc_chip', 'subscription');
