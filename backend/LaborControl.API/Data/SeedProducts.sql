-- Seed des 3 produits initiaux pour Labor Control

-- 1. Pack Découverte (one-time par client)
INSERT INTO "Products" ("Id", "Name", "Description", "Price", "ProductType", "Category", "IsOneTimePerCustomer", "StockQuantity", "ShippingCost", "IsActive", "DisplayOrder", "Metadata", "CreatedAt")
VALUES (
    '00000000-0000-0000-0000-000000000001',
    'Pack Découverte',
    'Abonnement Labor Control jusqu''à 10 points de contrôle GRATUIT + Pack 10 puces NFC pour Points de contrôles GRATUIT',
    0.00,
    'pack_discovery',
    'physical',
    true,  -- Un seul par client
    NULL,  -- Stock illimité
    10.00, -- Frais de livraison
    true,  -- Actif
    1,     -- Ordre d''affichage : premier
    '{"points_included": 10, "chips_included": 10}',
    NOW()
)
ON CONFLICT ("Id") DO NOTHING;

-- 2. Puces NFC supplémentaires (à l'unité)
INSERT INTO "Products" ("Id", "Name", "Description", "Price", "ProductType", "Category", "IsOneTimePerCustomer", "StockQuantity", "ShippingCost", "IsActive", "DisplayOrder", "Metadata", "CreatedAt")
VALUES (
    '00000000-0000-0000-0000-000000000002',
    'Puces NFC supplémentaires',
    'Puces NFC pour Points de contrôles supplémentaires - 80€ HT l''unité',
    80.00,
    'nfc_chip',
    'physical',
    false,  -- Peut acheter plusieurs fois
    NULL,   -- Stock illimité
    10.00,  -- Frais de livraison
    true,   -- Actif
    2,      -- Ordre d''affichage : deuxième
    '{"unit_type": "chip"}',
    NOW()
)
ON CONFLICT ("Id") DO NOTHING;

-- 3. Forfaits Abonnement Labor Control (subscriptions)
INSERT INTO "Products" ("Id", "Name", "Description", "Price", "ProductType", "Category", "IsOneTimePerCustomer", "StockQuantity", "ShippingCost", "IsActive", "DisplayOrder", "Metadata", "CreatedAt")
VALUES (
    '00000000-0000-0000-0000-000000000003',
    'Forfait Abonnement',
    'Abonnement Labor Control pour plus de 10 points de contrôle - Tarif progressif selon le nombre de points',
    0.00,  -- Prix de base, sera calculé selon le forfait choisi
    'subscription',
    'service',
    false,  -- Peut souscrire plusieurs fois (upgrades)
    NULL,   -- Pas de stock (service)
    NULL,   -- Pas de frais de livraison (service)
    true,   -- Actif
    3,      -- Ordre d''affichage : troisième
    '{"tiers": [{"max_points": 10, "price": 0, "name": "Gratuit"}, {"max_points": 25, "price": 49, "name": "Starter"}, {"max_points": 50, "price": 99, "name": "Professional"}, {"max_points": null, "price": null, "name": "Enterprise", "contact_required": true}]}',
    NOW()
)
ON CONFLICT ("Id") DO NOTHING;
