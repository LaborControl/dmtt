-- Seed predefined sectors for Labor Control
-- These sectors will be available to all customers but inactive by default
-- Customers can activate the sectors relevant to their business

-- Note: CustomerId will need to be set when executing this for a specific customer
-- Replace {{CUSTOMER_ID}} with the actual customer GUID

-- Clean up existing predefined sectors for this customer (if re-seeding)
-- DELETE FROM "Sectors" WHERE "CustomerId" = '{{CUSTOMER_ID}}' AND "IsPredefined" = true;

-- Maintenance industrielle
INSERT INTO "Sectors" ("Id", "CustomerId", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsPredefined", "IsActive", "CreatedAt")
VALUES (
    gen_random_uuid(),
    '{{CUSTOMER_ID}}',
    'Maintenance industrielle',
    'MAINTENANCE',
    'Maintenance préventive et curative des équipements industriels, électricité, mécanique, automatisme',
    '#3B82F6',
    '🔧',
    1,
    true,
    false,
    NOW()
) ON CONFLICT DO NOTHING;

-- QHSE (Qualité, Hygiène, Sécurité, Environnement)
INSERT INTO "Sectors" ("Id", "CustomerId", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsPredefined", "IsActive", "CreatedAt")
VALUES (
    gen_random_uuid(),
    '{{CUSTOMER_ID}}',
    'QHSE',
    'QHSE',
    'Qualité, Hygiène, Sécurité et Environnement - Prévention des risques professionnels',
    '#EF4444',
    '⚠️',
    2,
    true,
    false,
    NOW()
) ON CONFLICT DO NOTHING;

-- Santé et Médico-social
INSERT INTO "Sectors" ("Id", "CustomerId", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsPredefined", "IsActive", "CreatedAt")
VALUES (
    gen_random_uuid(),
    '{{CUSTOMER_ID}}',
    'Santé et Médico-social',
    'SANTE',
    'Secteur de la santé, aide à la personne, établissements médico-sociaux',
    '#10B981',
    '⚕️',
    3,
    true,
    false,
    NOW()
) ON CONFLICT DO NOTHING;

-- Nettoyage et Propreté
INSERT INTO "Sectors" ("Id", "CustomerId", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsPredefined", "IsActive", "CreatedAt")
VALUES (
    gen_random_uuid(),
    '{{CUSTOMER_ID}}',
    'Nettoyage et Propreté',
    'NETTOYAGE',
    'Services de nettoyage industriel, tertiaire, entretien des locaux',
    '#8B5CF6',
    '🧹',
    4,
    true,
    false,
    NOW()
) ON CONFLICT DO NOTHING;

-- Sécurité et Gardiennage
INSERT INTO "Sectors" ("Id", "CustomerId", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsPredefined", "IsActive", "CreatedAt")
VALUES (
    gen_random_uuid(),
    '{{CUSTOMER_ID}}',
    'Sécurité et Gardiennage',
    'SECURITE',
    'Agent de sécurité, gardiennage, surveillance, sûreté',
    '#F59E0B',
    '🛡️',
    5,
    true,
    false,
    NOW()
) ON CONFLICT DO NOTHING;

-- Commerce et Vente
INSERT INTO "Sectors" ("Id", "CustomerId", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsPredefined", "IsActive", "CreatedAt")
VALUES (
    gen_random_uuid(),
    '{{CUSTOMER_ID}}',
    'Commerce et Vente',
    'COMMERCE',
    'Grande distribution, commerce de détail, vente',
    '#EC4899',
    '🛒',
    6,
    true,
    false,
    NOW()
) ON CONFLICT DO NOTHING;

-- Restauration et Hôtellerie
INSERT INTO "Sectors" ("Id", "CustomerId", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsPredefined", "IsActive", "CreatedAt")
VALUES (
    gen_random_uuid(),
    '{{CUSTOMER_ID}}',
    'Restauration et Hôtellerie',
    'RESTAURATION',
    'Restauration collective, restauration rapide, hôtellerie',
    '#F97316',
    '🍽️',
    7,
    true,
    false,
    NOW()
) ON CONFLICT DO NOTHING;

-- Logistique et Transport
INSERT INTO "Sectors" ("Id", "CustomerId", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsPredefined", "IsActive", "CreatedAt")
VALUES (
    gen_random_uuid(),
    '{{CUSTOMER_ID}}',
    'Logistique et Transport',
    'LOGISTIQUE',
    'Entrepôt, préparation de commandes, manutention, livraison',
    '#06B6D4',
    '📦',
    8,
    true,
    false,
    NOW()
) ON CONFLICT DO NOTHING;

-- BTP (Bâtiment et Travaux Publics)
INSERT INTO "Sectors" ("Id", "CustomerId", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsPredefined", "IsActive", "CreatedAt")
VALUES (
    gen_random_uuid(),
    '{{CUSTOMER_ID}}',
    'BTP',
    'BTP',
    'Bâtiment, travaux publics, génie civil, construction',
    '#6366F1',
    '🏗️',
    9,
    true,
    false,
    NOW()
) ON CONFLICT DO NOTHING;

-- Informatique et Digital
INSERT INTO "Sectors" ("Id", "CustomerId", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsPredefined", "IsActive", "CreatedAt")
VALUES (
    gen_random_uuid(),
    '{{CUSTOMER_ID}}',
    'Informatique et Digital',
    'IT',
    'Support informatique, développement, infrastructure IT',
    '#14B8A6',
    '💻',
    10,
    true,
    false,
    NOW()
) ON CONFLICT DO NOTHING;

SELECT 'Predefined sectors seeded successfully for customer {{CUSTOMER_ID}}' AS message;
