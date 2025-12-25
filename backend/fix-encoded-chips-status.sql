-- Correction du statut des puces déjà encodées
-- Ces puces ont Salt et Checksum renseignés mais sont restées en EN_ATELIER
-- Elles doivent passer en EN_STOCK

UPDATE "RfidChips"
SET "Status" = 'EN_STOCK',
    "UpdatedAt" = NOW()
WHERE "Salt" IS NOT NULL
  AND "Checksum" IS NOT NULL
  AND "Status" = 'EN_ATELIER';

-- Afficher le nombre de puces mises à jour
SELECT
    COUNT(*) as "NombrePucesCorigees",
    'Les puces avec Salt/Checksum ont été passées en EN_STOCK' as "Message"
FROM "RfidChips"
WHERE "Salt" IS NOT NULL
  AND "Checksum" IS NOT NULL
  AND "Status" = 'EN_STOCK';
