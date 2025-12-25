-- Insérer la gamme de maintenance "Contrôle Visuel" par défaut
-- Cette gamme est affectée automatiquement si aucune gamme n'est sélectionnée
-- IMPORTANT: Utilise le premier CustomerId disponible (IsUniversal = true la rend accessible à tous)

DO $$
DECLARE
    first_customer_id UUID;
BEGIN
    -- Récupérer le premier CustomerId disponible
    SELECT "Id" INTO first_customer_id FROM "Customers" ORDER BY "CreatedAt" LIMIT 1;

    -- Insérer ou mettre à jour la gamme "Contrôle Visuel"
    INSERT INTO "TaskTemplates" ("Id", "CustomerId", "Name", "Category", "RequiredQualification", "IsUniversal", "AlertOnMismatch", "FormTemplate", "IsActive", "CreatedAt")
    VALUES (
        'a0000000-0000-0000-0000-000000000001', -- ID fixe pour la gamme par défaut
        first_customer_id, -- Premier client (IsUniversal = true la rend accessible à tous)
        'Contrôle Visuel',
        'SURVEILLANCE',
        'AUCUNE',
        true, -- IsUniversal = true (disponible pour tous les clients)
        true, -- AlertOnMismatch = true
        '{}', -- FormTemplate vide par défaut
        true, -- IsActive = true
        NOW()
    )
    ON CONFLICT ("Id") DO UPDATE SET
        "Name" = EXCLUDED."Name",
        "Category" = EXCLUDED."Category",
        "RequiredQualification" = EXCLUDED."RequiredQualification",
        "IsUniversal" = EXCLUDED."IsUniversal",
        "AlertOnMismatch" = EXCLUDED."AlertOnMismatch",
        "IsActive" = EXCLUDED."IsActive";
END $$;
