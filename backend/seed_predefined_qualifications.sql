-- Seed predefined qualifications for Labor Control
-- Includes RNCP certifications and RS certifications (habilitations, CACES, etc.)

-- Sector IDs for reference:
-- QHSE: 11111111-1111-1111-1111-111111111101
-- SANTE: 11111111-1111-1111-1111-111111111102
-- NETTOYAGE: 11111111-1111-1111-1111-111111111103
-- SECURITE: 11111111-1111-1111-1111-111111111104
-- COMMERCE: 11111111-1111-1111-1111-111111111105
-- RESTAURATION: 11111111-1111-1111-1111-111111111106
-- LOGISTIQUE: 11111111-1111-1111-1111-111111111107
-- BTP: 11111111-1111-1111-1111-111111111108
-- IT: 11111111-1111-1111-1111-111111111109
-- MAINTENANCE: 11111111-1111-1111-1111-111111111110

-- QualificationType enum values:
-- 0 = Custom
-- 1 = RNCP
-- 2 = RS (Répertoire Spécifique)
-- 3 = CQP
-- 4 = Habilitation

-- ================================================
-- HABILITATIONS ET CERTIFICATIONS TRANSVERSALES (RS)
-- ================================================

-- SST - Sauveteur Secouriste du Travail (multisecteur)
INSERT INTO "PredefinedQualifications" ("Id", "Name", "Code", "Description", "Type", "RsCode", "FranceCompetencesUrl", "Certificateur", "DateEnregistrement", "DateFinValidite", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES (
    '33333333-3333-3333-3333-333333333301',
    'Sauveteur secouriste du travail (SST)',
    'SST',
    'Formation aux premiers secours en entreprise - Certificat obligatoire',
    2, -- RS
    'RS5226',
    'https://www.francecompetences.fr/recherche/rs/5226',
    'INRS',
    '2019-12-18',
    '2024-12-18',
    '#DC2626',
    '🚑',
    1,
    true,
    NOW()
) ON CONFLICT DO NOTHING;

-- SST appartient à tous les secteurs
INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333301', '11111111-1111-1111-1111-111111111101') ON CONFLICT DO NOTHING;
INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333301', '11111111-1111-1111-1111-111111111102') ON CONFLICT DO NOTHING;
INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333301', '11111111-1111-1111-1111-111111111103') ON CONFLICT DO NOTHING;
INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333301', '11111111-1111-1111-1111-111111111104') ON CONFLICT DO NOTHING;
INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333301', '11111111-1111-1111-1111-111111111106') ON CONFLICT DO NOTHING;
INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333301', '11111111-1111-1111-1111-111111111107') ON CONFLICT DO NOTHING;
INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333301', '11111111-1111-1111-1111-111111111108') ON CONFLICT DO NOTHING;
INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333301', '11111111-1111-1111-1111-111111111110') ON CONFLICT DO NOTHING;

-- Habilitation électrique B1V-BR (MAINTENANCE, BTP, IT)
INSERT INTO "PredefinedQualifications" ("Id", "Name", "Code", "Description", "Type", "RsCode", "FranceCompetencesUrl", "Certificateur", "DateEnregistrement", "DateFinValidite", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES (
    '33333333-3333-3333-3333-333333333302',
    'Habilitation électrique B1V-BR',
    'HAB-ELEC-BT',
    'Habilitation pour travaux d''ordre électrique en basse tension',
    4, -- Habilitation
    'RS6401',
    'https://www.francecompetences.fr/recherche/rs/6401',
    'INRS',
    '2023-01-01',
    '2028-01-01',
    '#F59E0B',
    '⚡',
    2,
    true,
    NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333302', '11111111-1111-1111-1111-111111111108') ON CONFLICT DO NOTHING;
INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333302', '11111111-1111-1111-1111-111111111109') ON CONFLICT DO NOTHING;
INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333302', '11111111-1111-1111-1111-111111111110') ON CONFLICT DO NOTHING;

-- CACES R489 - Chariots élévateurs (LOGISTIQUE, COMMERCE)
INSERT INTO "PredefinedQualifications" ("Id", "Name", "Code", "Description", "Type", "RsCode", "FranceCompetencesUrl", "Certificateur", "DateEnregistrement", "DateFinValidite", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES (
    '33333333-3333-3333-3333-333333333303',
    'CACES R489 - Chariots de manutention automoteurs',
    'CACES-R489',
    'Conduite en sécurité des chariots élévateurs à conducteur porté',
    4, -- Habilitation
    'RS5414',
    'https://www.francecompetences.fr/recherche/rs/5414',
    'CNAMTS',
    '2020-01-01',
    '2025-01-01',
    '#06B6D4',
    '🚜',
    3,
    true,
    NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333303', '11111111-1111-1111-1111-111111111105') ON CONFLICT DO NOTHING;
INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333303', '11111111-1111-1111-1111-111111111107') ON CONFLICT DO NOTHING;

-- CACES R486 - Nacelles PEMP (BTP, MAINTENANCE)
INSERT INTO "PredefinedQualifications" ("Id", "Name", "Code", "Description", "Type", "RsCode", "FranceCompetencesUrl", "Certificateur", "DateEnregistrement", "DateFinValidite", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES (
    '33333333-3333-3333-3333-333333333304',
    'CACES R486 - Plateformes élévatrices mobiles de personnel',
    'CACES-R486',
    'Conduite en sécurité des nacelles élévatrices (PEMP)',
    4, -- Habilitation
    'RS5424',
    'https://www.francecompetences.fr/recherche/rs/5424',
    'CNAMTS',
    '2020-01-01',
    '2025-01-01',
    '#8B5CF6',
    '🏗️',
    4,
    true,
    NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333304', '11111111-1111-1111-1111-111111111108') ON CONFLICT DO NOTHING;
INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333304', '11111111-1111-1111-1111-111111111110') ON CONFLICT DO NOTHING;

-- CACES R482 - Engins de chantier (BTP)
INSERT INTO "PredefinedQualifications" ("Id", "Name", "Code", "Description", "Type", "RsCode", "FranceCompetencesUrl", "Certificateur", "DateEnregistrement", "DateFinValidite", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES (
    '33333333-3333-3333-3333-333333333305',
    'CACES R482 - Engins de chantier',
    'CACES-R482',
    'Conduite en sécurité des engins de chantier',
    4, -- Habilitation
    'RS5433',
    'https://www.francecompetences.fr/recherche/rs/5433',
    'CNAMTS',
    '2020-01-01',
    '2025-01-01',
    '#6366F1',
    '🚧',
    5,
    true,
    NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333305', '11111111-1111-1111-1111-111111111108') ON CONFLICT DO NOTHING;

-- SSIAP 1 - Sécurité incendie (SECURITE)
INSERT INTO "PredefinedQualifications" ("Id", "Name", "Code", "Description", "Type", "RsCode", "FranceCompetencesUrl", "Certificateur", "DateEnregistrement", "DateFinValidite", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES (
    '33333333-3333-3333-3333-333333333306',
    'SSIAP 1 - Agent de sécurité incendie',
    'SSIAP1',
    'Service de sécurité incendie et d''assistance aux personnes en ERP et IGH',
    4, -- Habilitation
    'RS5748',
    'https://www.francecompetences.fr/recherche/rs/5748',
    'Ministère de l''Intérieur',
    '2021-03-10',
    '2026-03-10',
    '#EF4444',
    '🔥',
    6,
    true,
    NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333306', '11111111-1111-1111-1111-111111111104') ON CONFLICT DO NOTHING;

-- SSIAP 2 - Chef d'équipe sécurité incendie (SECURITE)
INSERT INTO "PredefinedQualifications" ("Id", "Name", "Code", "Description", "Type", "RsCode", "FranceCompetencesUrl", "Certificateur", "DateEnregistrement", "DateFinValidite", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES (
    '33333333-3333-3333-3333-333333333307',
    'SSIAP 2 - Chef d''équipe sécurité incendie',
    'SSIAP2',
    'Chef d''équipe de sécurité incendie et d''assistance aux personnes',
    4, -- Habilitation
    'RS5749',
    'https://www.francecompetences.fr/recherche/rs/5749',
    'Ministère de l''Intérieur',
    '2021-03-10',
    '2026-03-10',
    '#DC2626',
    '🧯',
    7,
    true,
    NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333307', '11111111-1111-1111-1111-111111111104') ON CONFLICT DO NOTHING;

-- HACCP - Hygiène alimentaire (RESTAURATION)
INSERT INTO "PredefinedQualifications" ("Id", "Name", "Code", "Description", "Type", "RsCode", "FranceCompetencesUrl", "Certificateur", "DateEnregistrement", "DateFinValidite", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES (
    '33333333-3333-3333-3333-333333333308',
    'Bonnes pratiques d''hygiène en restauration commerciale',
    'HACCP',
    'Formation HACCP obligatoire en restauration',
    2, -- RS
    'RS6128',
    'https://www.francecompetences.fr/recherche/rs/6128',
    'Ministère de l''Agriculture',
    '2022-09-29',
    '2027-09-29',
    '#F97316',
    '🍽️',
    8,
    true,
    NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333308', '11111111-1111-1111-1111-111111111106') ON CONFLICT DO NOTHING;

-- Travail en hauteur (BTP, MAINTENANCE)
INSERT INTO "PredefinedQualifications" ("Id", "Name", "Code", "Description", "Type", "RsCode", "FranceCompetencesUrl", "Certificateur", "DateEnregistrement", "DateFinValidite", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES (
    '33333333-3333-3333-3333-333333333309',
    'Travail en hauteur - Utilisation des EPI contre les chutes',
    'TRAVAIL-HAUTEUR',
    'Formation travail en hauteur avec harnais et ligne de vie',
    4, -- Habilitation
    'RS5512',
    'https://www.francecompetences.fr/recherche/rs/5512',
    'OPPBTP',
    '2020-04-30',
    '2025-04-30',
    '#9333EA',
    '⛑️',
    9,
    true,
    NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333309', '11111111-1111-1111-1111-111111111108') ON CONFLICT DO NOTHING;
INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333309', '11111111-1111-1111-1111-111111111110') ON CONFLICT DO NOTHING;

-- ================================================
-- CERTIFICATIONS RNCP
-- ================================================

-- Manager QHSE - Niveau 7 (QHSE)
INSERT INTO "PredefinedQualifications" ("Id", "Name", "Code", "Description", "Type", "RncpCode", "FranceCompetencesUrl", "Level", "Certificateur", "DateEnregistrement", "DateFinValidite", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES (
    '33333333-3333-3333-3333-333333333310',
    'Manager en santé, sécurité et environnement au travail',
    'MANAGER-QHSE',
    'Formation aux métiers de la prévention des risques professionnels - Niveau Master',
    1, -- RNCP
    'RNCP36368',
    'https://www.francecompetences.fr/recherche/rncp/36368',
    7,
    'Institut de formation en management des risques',
    '2022-03-25',
    '2027-03-25',
    '#EF4444',
    '👔',
    10,
    true,
    NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333310', '11111111-1111-1111-1111-111111111101') ON CONFLICT DO NOTHING;

-- Responsable QHSE - Niveau 6 (QHSE)
INSERT INTO "PredefinedQualifications" ("Id", "Name", "Code", "Description", "Type", "RncpCode", "FranceCompetencesUrl", "Level", "Certificateur", "DateEnregistrement", "DateFinValidite", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES (
    '33333333-3333-3333-3333-333333333311',
    'Responsable qualité hygiène sécurité environnement',
    'RESP-QHSE',
    'Responsable QHSE en entreprise industrielle ou tertiaire - Niveau Licence',
    1, -- RNCP
    'RNCP36610',
    'https://www.francecompetences.fr/recherche/rncp/36610',
    6,
    'CNAM',
    '2022-07-01',
    '2027-07-01',
    '#DC2626',
    '📋',
    11,
    true,
    NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333311', '11111111-1111-1111-1111-111111111101') ON CONFLICT DO NOTHING;

-- Technicien maintenance - Niveau 5 (MAINTENANCE)
INSERT INTO "PredefinedQualifications" ("Id", "Name", "Code", "Description", "Type", "RncpCode", "FranceCompetencesUrl", "Level", "Certificateur", "DateEnregistrement", "DateFinValidite", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES (
    '33333333-3333-3333-3333-333333333312',
    'Technicien supérieur en maintenance industrielle',
    'TECH-MAINTENANCE',
    'BTS Maintenance des systèmes option systèmes de production',
    1, -- RNCP
    'RNCP35191',
    'https://www.francecompetences.fr/recherche/rncp/35191',
    5,
    'Ministère de l''Enseignement supérieur',
    '2020-11-30',
    '2025-11-30',
    '#3B82F6',
    '🔧',
    12,
    true,
    NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333312', '11111111-1111-1111-1111-111111111110') ON CONFLICT DO NOTHING;

-- Aide-soignant - Niveau 4 (SANTE)
INSERT INTO "PredefinedQualifications" ("Id", "Name", "Code", "Description", "Type", "RncpCode", "FranceCompetencesUrl", "Level", "Certificateur", "DateEnregistrement", "DateFinValidite", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES (
    '33333333-3333-3333-3333-333333333313',
    'Diplôme d''État d''aide-soignant',
    'DEAS',
    'Aide-soignant en établissement de santé',
    1, -- RNCP
    'RNCP4495',
    'https://www.francecompetences.fr/recherche/rncp/4495',
    4,
    'Ministère de la Santé',
    '2021-06-10',
    '2026-06-10',
    '#10B981',
    '💊',
    13,
    true,
    NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333313', '11111111-1111-1111-1111-111111111102') ON CONFLICT DO NOTHING;

-- Infirmier - Niveau 6 (SANTE)
INSERT INTO "PredefinedQualifications" ("Id", "Name", "Code", "Description", "Type", "RncpCode", "FranceCompetencesUrl", "Level", "Certificateur", "DateEnregistrement", "DateFinValidite", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES (
    '33333333-3333-3333-3333-333333333314',
    'Diplôme d''État d''infirmier',
    'DEI',
    'Infirmier diplômé d''État',
    1, -- RNCP
    'RNCP492',
    'https://www.francecompetences.fr/recherche/rncp/492',
    6,
    'Ministère de la Santé',
    '2020-01-01',
    NULL, -- Pas de date de fin
    '#059669',
    '⚕️',
    14,
    true,
    NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333314', '11111111-1111-1111-1111-111111111102') ON CONFLICT DO NOTHING;

-- Agent de propreté - Niveau 3 (NETTOYAGE)
INSERT INTO "PredefinedQualifications" ("Id", "Name", "Code", "Description", "Type", "RncpCode", "FranceCompetencesUrl", "Level", "Certificateur", "DateEnregistrement", "DateFinValidite", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES (
    '33333333-3333-3333-3333-333333333315',
    'Agent de propreté et d''hygiène',
    'APH',
    'Agent de nettoyage en milieu professionnel',
    1, -- RNCP
    'RNCP35750',
    'https://www.francecompetences.fr/recherche/rncp/35750',
    3,
    'Ministère du Travail',
    '2021-05-21',
    '2026-05-21',
    '#8B5CF6',
    '🧹',
    15,
    true,
    NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333315', '11111111-1111-1111-1111-111111111103') ON CONFLICT DO NOTHING;

-- Agent de sécurité APS - Niveau 3 (SECURITE)
INSERT INTO "PredefinedQualifications" ("Id", "Name", "Code", "Description", "Type", "RncpCode", "FranceCompetencesUrl", "Level", "Certificateur", "DateEnregistrement", "DateFinValidite", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES (
    '33333333-3333-3333-3333-333333333316',
    'Agent de prévention et de sécurité',
    'APS',
    'Agent de sécurité privée CQP APS',
    1, -- RNCP
    'RNCP34507',
    'https://www.francecompetences.fr/recherche/rncp/34507',
    3,
    'Ministère du Travail',
    '2020-02-21',
    '2025-02-21',
    '#F59E0B',
    '🛡️',
    16,
    true,
    NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333316', '11111111-1111-1111-1111-111111111104') ON CONFLICT DO NOTHING;

-- Responsable logistique - Niveau 6 (LOGISTIQUE)
INSERT INTO "PredefinedQualifications" ("Id", "Name", "Code", "Description", "Type", "RncpCode", "FranceCompetencesUrl", "Level", "Certificateur", "DateEnregistrement", "DateFinValidite", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES (
    '33333333-3333-3333-3333-333333333317',
    'Responsable logistique',
    'RESP-LOG',
    'Manager de la supply chain et de la logistique',
    1, -- RNCP
    'RNCP35869',
    'https://www.francecompetences.fr/recherche/rncp/35869',
    6,
    'AFTRAL',
    '2021-07-08',
    '2026-07-08',
    '#06B6D4',
    '📦',
    17,
    true,
    NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333317', '11111111-1111-1111-1111-111111111107') ON CONFLICT DO NOTHING;

-- Conducteur de travaux BTP - Niveau 6 (BTP)
INSERT INTO "PredefinedQualifications" ("Id", "Name", "Code", "Description", "Type", "RncpCode", "FranceCompetencesUrl", "Level", "Certificateur", "DateEnregistrement", "DateFinValidite", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES (
    '33333333-3333-3333-3333-333333333318',
    'Conducteur de travaux du bâtiment et du génie civil',
    'CONDUCTEUR-TRAVAUX',
    'Conducteur de travaux en construction',
    1, -- RNCP
    'RNCP35315',
    'https://www.francecompetences.fr/recherche/rncp/35315',
    6,
    'ESTP',
    '2021-02-10',
    '2026-02-10',
    '#6366F1',
    '👷',
    18,
    true,
    NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333318', '11111111-1111-1111-1111-111111111108') ON CONFLICT DO NOTHING;

-- Administrateur systèmes - Niveau 6 (IT)
INSERT INTO "PredefinedQualifications" ("Id", "Name", "Code", "Description", "Type", "RncpCode", "FranceCompetencesUrl", "Level", "Certificateur", "DateEnregistrement", "DateFinValidite", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES (
    '33333333-3333-3333-3333-333333333319',
    'Administrateur d''infrastructures sécurisées',
    'ADMIN-SYS',
    'Administrateur systèmes et réseaux',
    1, -- RNCP
    'RNCP36137',
    'https://www.francecompetences.fr/recherche/rncp/36137',
    6,
    'Ministère du Travail',
    '2021-12-01',
    '2026-12-01',
    '#14B8A6',
    '💻',
    19,
    true,
    NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333319', '11111111-1111-1111-1111-111111111109') ON CONFLICT DO NOTHING;

-- Préparateur de commandes - Niveau 3 (LOGISTIQUE)
INSERT INTO "PredefinedQualifications" ("Id", "Name", "Code", "Description", "Type", "RncpCode", "FranceCompetencesUrl", "Level", "Certificateur", "DateEnregistrement", "DateFinValidite", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES (
    '33333333-3333-3333-3333-333333333320',
    'Préparateur de commandes en entrepôt',
    'PREP-COMMANDES',
    'Préparateur de commandes avec CACES 1 et 3',
    1, -- RNCP
    'RNCP36375',
    'https://www.francecompetences.fr/recherche/rncp/36375',
    3,
    'Ministère du Travail',
    '2022-03-25',
    '2027-03-25',
    '#0891B2',
    '📋',
    20,
    true,
    NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "PredefinedQualificationSectors" VALUES ('33333333-3333-3333-3333-333333333320', '11111111-1111-1111-1111-111111111107') ON CONFLICT DO NOTHING;

SELECT 'Predefined qualifications seeded successfully' AS message;
SELECT COUNT(*) AS total_qualifications FROM "PredefinedQualifications";
SELECT COUNT(*) AS total_sector_associations FROM "PredefinedQualificationSectors";
