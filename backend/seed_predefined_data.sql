-- Seed Predefined Sectors and Industries for Labor Control
-- This script inserts all predefined data into the database

-- First, clear existing data (if any)
DELETE FROM "PredefinedIndustries";
DELETE FROM "PredefinedSectors";

-- ========================================
-- PREDEFINED SECTORS
-- ========================================

-- QHSE
INSERT INTO "PredefinedSectors" ("Id", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES ('11111111-1111-1111-1111-111111111101', 'QHSE', 'QHSE', 'Qualité, Hygiène, Sécurité et Environnement', '#EF4444', '⚠️', 1, true, NOW());

-- Santé et Médico-social
INSERT INTO "PredefinedSectors" ("Id", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES ('11111111-1111-1111-1111-111111111102', 'Santé et Médico-social', 'SANTE', 'Secteur de la santé, aide à la personne, établissements médico-sociaux', '#10B981', '⚕️', 2, true, NOW());

-- Nettoyage et Propreté
INSERT INTO "PredefinedSectors" ("Id", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES ('11111111-1111-1111-1111-111111111103', 'Nettoyage et Propreté', 'NETTOYAGE', 'Services de nettoyage industriel, tertiaire, entretien des locaux', '#8B5CF6', '🧹', 3, true, NOW());

-- Sécurité et Gardiennage
INSERT INTO "PredefinedSectors" ("Id", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES ('11111111-1111-1111-1111-111111111104', 'Sécurité et Gardiennage', 'SECURITE', 'Agent de sécurité, gardiennage, surveillance, sûreté', '#F59E0B', '🛡️', 4, true, NOW());

-- Commerce et Vente
INSERT INTO "PredefinedSectors" ("Id", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES ('11111111-1111-1111-1111-111111111105', 'Commerce et Vente', 'COMMERCE', 'Grande distribution, commerce de détail, vente', '#EC4899', '🛒', 5, true, NOW());

-- Restauration et Hôtellerie
INSERT INTO "PredefinedSectors" ("Id", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES ('11111111-1111-1111-1111-111111111106', 'Restauration et Hôtellerie', 'RESTAURATION', 'Restauration collective, restauration rapide, hôtellerie', '#F97316', '🍽️', 6, true, NOW());

-- Logistique et Transport
INSERT INTO "PredefinedSectors" ("Id", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES ('11111111-1111-1111-1111-111111111107', 'Logistique et Transport', 'LOGISTIQUE', 'Entrepôt, préparation de commandes, manutention, livraison', '#06B6D4', '📦', 7, true, NOW());

-- BTP
INSERT INTO "PredefinedSectors" ("Id", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES ('11111111-1111-1111-1111-111111111108', 'BTP', 'BTP', 'Bâtiment, travaux publics, génie civil, construction', '#6366F1', '🏗️', 8, true, NOW());

-- Informatique et Digital
INSERT INTO "PredefinedSectors" ("Id", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES ('11111111-1111-1111-1111-111111111109', 'Informatique et Digital', 'IT', 'Support informatique, développement, infrastructure IT', '#14B8A6', '💻', 9, true, NOW());

-- Maintenance industrielle
INSERT INTO "PredefinedSectors" ("Id", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES ('11111111-1111-1111-1111-111111111110', 'Maintenance industrielle', 'MAINTENANCE', 'Maintenance préventive et curative des équipements industriels', '#3B82F6', '🔧', 10, true, NOW());

-- ========================================
-- PREDEFINED INDUSTRIES (METIERS)
-- ========================================

-- QHSE Industries
INSERT INTO "PredefinedIndustries" ("Id", "PredefinedSectorId", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES
('22222222-2222-2222-2222-222222222201', '11111111-1111-1111-1111-111111111101', 'Responsable QHSE', 'RESP_QHSE', 'Responsable Qualité Hygiène Sécurité Environnement', '#DC2626', '👔', 1, true, NOW()),
('22222222-2222-2222-2222-222222222202', '11111111-1111-1111-1111-111111111101', 'Animateur Sécurité', 'ANIM_SEC', 'Animation et prévention sécurité sur site', '#EF4444', '🎯', 2, true, NOW()),
('22222222-2222-2222-2222-222222222203', '11111111-1111-1111-1111-111111111101', 'Auditeur Qualité', 'AUDIT_Q', 'Réalisation d''audits qualité internes et externes', '#F87171', '📋', 3, true, NOW()),
('22222222-2222-2222-2222-222222222204', '11111111-1111-1111-1111-111111111101', 'Technicien HSE', 'TECH_HSE', 'Technicien Hygiène Sécurité Environnement', '#FCA5A5', '🔍', 4, true, NOW());

-- Santé Industries
INSERT INTO "PredefinedIndustries" ("Id", "PredefinedSectorId", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES
('22222222-2222-2222-2222-222222222205', '11111111-1111-1111-1111-111111111102', 'Aide-soignant(e)', 'AS', 'Assistance aux soins quotidiens des patients', '#059669', '💊', 1, true, NOW()),
('22222222-2222-2222-2222-222222222206', '11111111-1111-1111-1111-111111111102', 'Infirmier(e)', 'IDE', 'Soins infirmiers et suivi médical', '#10B981', '💉', 2, true, NOW()),
('22222222-2222-2222-2222-222222222207', '11111111-1111-1111-1111-111111111102', 'Auxiliaire de vie', 'AVS', 'Aide à la personne dépendante', '#34D399', '🤝', 3, true, NOW()),
('22222222-2222-2222-2222-222222222208', '11111111-1111-1111-1111-111111111102', 'Agent de service hospitalier', 'ASH', 'Nettoyage et entretien en milieu hospitalier', '#6EE7B7', '🏥', 4, true, NOW());

-- Nettoyage Industries
INSERT INTO "PredefinedIndustries" ("Id", "PredefinedSectorId", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES
('22222222-2222-2222-2222-222222222209', '11111111-1111-1111-1111-111111111103', 'Agent de nettoyage', 'AGENT_NET', 'Nettoyage et entretien des locaux', '#7C3AED', '🧽', 1, true, NOW()),
('22222222-2222-2222-2222-222222222210', '11111111-1111-1111-1111-111111111103', 'Agent de propreté urbaine', 'APU', 'Nettoyage des espaces publics et voirie', '#8B5CF6', '🌳', 2, true, NOW()),
('22222222-2222-2222-2222-222222222211', '11111111-1111-1111-1111-111111111103', 'Chef d''équipe nettoyage', 'CHEF_NET', 'Supervision des équipes de nettoyage', '#A78BFA', '👷', 3, true, NOW());

-- Sécurité Industries
INSERT INTO "PredefinedIndustries" ("Id", "PredefinedSectorId", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES
('22222222-2222-2222-2222-222222222212', '11111111-1111-1111-1111-111111111104', 'Agent de sécurité', 'ADS', 'Surveillance et protection des biens et personnes', '#D97706', '🔒', 1, true, NOW()),
('22222222-2222-2222-2222-222222222213', '11111111-1111-1111-1111-111111111104', 'Agent cynophile', 'CYNO', 'Agent de sécurité avec maître-chien', '#F59E0B', '🐕', 2, true, NOW()),
('22222222-2222-2222-2222-222222222214', '11111111-1111-1111-1111-111111111104', 'Agent SSIAP', 'SSIAP', 'Sécurité incendie et assistance aux personnes', '#FBBF24', '🔥', 3, true, NOW()),
('22222222-2222-2222-2222-222222222215', '11111111-1111-1111-1111-111111111104', 'Rondier intervenant', 'RONDIER', 'Rondes de surveillance et interventions', '#FCD34D', '🚶', 4, true, NOW());

-- Commerce Industries
INSERT INTO "PredefinedIndustries" ("Id", "PredefinedSectorId", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES
('22222222-2222-2222-2222-222222222216', '11111111-1111-1111-1111-111111111105', 'Vendeur(se)', 'VENDEUR', 'Conseil et vente aux clients', '#DB2777', '🏷️', 1, true, NOW()),
('22222222-2222-2222-2222-222222222217', '11111111-1111-1111-1111-111111111105', 'Caissier(ère)', 'CAISSIER', 'Encaissement et relation client', '#EC4899', '💳', 2, true, NOW()),
('22222222-2222-2222-2222-222222222218', '11111111-1111-1111-1111-111111111105', 'Employé(e) commercial(e)', 'EMP_COM', 'Mise en rayon et gestion des stocks', '#F472B6', '📦', 3, true, NOW()),
('22222222-2222-2222-2222-222222222219', '11111111-1111-1111-1111-111111111105', 'Chef de rayon', 'CHEF_RAYON', 'Gestion et animation d''un rayon', '#F9A8D4', '📊', 4, true, NOW());

-- Restauration Industries
INSERT INTO "PredefinedIndustries" ("Id", "PredefinedSectorId", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES
('22222222-2222-2222-2222-222222222220', '11111111-1111-1111-1111-111111111106', 'Cuisinier(ère)', 'CUISINIER', 'Préparation des repas et gestion cuisine', '#C2410C', '👨‍🍳', 1, true, NOW()),
('22222222-2222-2222-2222-222222222221', '11111111-1111-1111-1111-111111111106', 'Serveur(se)', 'SERVEUR', 'Service en salle et relation client', '#EA580C', '🍷', 2, true, NOW()),
('22222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111106', 'Commis de cuisine', 'COMMIS', 'Aide en cuisine et préparation', '#F97316', '🥘', 3, true, NOW()),
('22222222-2222-2222-2222-222222222223', '11111111-1111-1111-1111-111111111106', 'Plongeur', 'PLONGEUR', 'Nettoyage de la vaisselle et de la cuisine', '#FB923C', '🍽️', 4, true, NOW());

-- Logistique Industries
INSERT INTO "PredefinedIndustries" ("Id", "PredefinedSectorId", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES
('22222222-2222-2222-2222-222222222224', '11111111-1111-1111-1111-111111111107', 'Cariste', 'CARISTE', 'Conduite d''engins de manutention', '#0891B2', '🚜', 1, true, NOW()),
('22222222-2222-2222-2222-222222222225', '11111111-1111-1111-1111-111111111107', 'Préparateur de commandes', 'PREP_CMD', 'Préparation et conditionnement des commandes', '#06B6D4', '📋', 2, true, NOW()),
('22222222-2222-2222-2222-222222222226', '11111111-1111-1111-1111-111111111107', 'Magasinier', 'MAGASINIER', 'Gestion des stocks et réception marchandises', '#22D3EE', '🏭', 3, true, NOW()),
('22222222-2222-2222-2222-222222222227', '11111111-1111-1111-1111-111111111107', 'Agent de quai', 'AGENT_QUAI', 'Chargement et déchargement des marchandises', '#67E8F9', '🚚', 4, true, NOW());

-- BTP Industries
INSERT INTO "PredefinedIndustries" ("Id", "PredefinedSectorId", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES
('22222222-2222-2222-2222-222222222228', '11111111-1111-1111-1111-111111111108', 'Maçon', 'MACON', 'Construction et maçonnerie', '#4F46E5', '🧱', 1, true, NOW()),
('22222222-2222-2222-2222-222222222229', '11111111-1111-1111-1111-111111111108', 'Électricien bâtiment', 'ELEC_BAT', 'Installation électrique du bâtiment', '#6366F1', '⚡', 2, true, NOW()),
('22222222-2222-2222-2222-222222222230', '11111111-1111-1111-1111-111111111108', 'Plombier', 'PLOMBIER', 'Installation et maintenance plomberie', '#818CF8', '🔧', 3, true, NOW()),
('22222222-2222-2222-2222-222222222231', '11111111-1111-1111-1111-111111111108', 'Peintre en bâtiment', 'PEINTRE', 'Travaux de peinture et finitions', '#A5B4FC', '🎨', 4, true, NOW()),
('22222222-2222-2222-2222-222222222232', '11111111-1111-1111-1111-111111111108', 'Charpentier', 'CHARPENTIER', 'Construction de charpentes bois', '#C7D2FE', '🪵', 5, true, NOW());

-- IT Industries
INSERT INTO "PredefinedIndustries" ("Id", "PredefinedSectorId", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES
('22222222-2222-2222-2222-222222222233', '11111111-1111-1111-1111-111111111109', 'Technicien support', 'TECH_SUPPORT', 'Support informatique utilisateurs', '#0D9488', '🖥️', 1, true, NOW()),
('22222222-2222-2222-2222-222222222234', '11111111-1111-1111-1111-111111111109', 'Administrateur système', 'ADMIN_SYS', 'Gestion des systèmes et serveurs', '#14B8A6', '🖧', 2, true, NOW()),
('22222222-2222-2222-2222-222222222235', '11111111-1111-1111-1111-111111111109', 'Développeur', 'DEV', 'Développement logiciel et applications', '#2DD4BF', '💻', 3, true, NOW()),
('22222222-2222-2222-2222-222222222236', '11111111-1111-1111-1111-111111111109', 'Technicien réseau', 'TECH_RESEAU', 'Installation et maintenance réseau', '#5EEAD4', '🌐', 4, true, NOW());

-- Maintenance Industries
INSERT INTO "PredefinedIndustries" ("Id", "PredefinedSectorId", "Name", "Code", "Description", "Color", "Icon", "DisplayOrder", "IsActive", "CreatedAt")
VALUES
('22222222-2222-2222-2222-222222222237', '11111111-1111-1111-1111-111111111110', 'Technicien de maintenance', 'TECH_MAINT', 'Maintenance préventive et corrective', '#2563EB', '🛠️', 1, true, NOW()),
('22222222-2222-2222-2222-222222222238', '11111111-1111-1111-1111-111111111110', 'Électricien industriel', 'ELEC_IND', 'Maintenance électrique industrielle', '#3B82F6', '⚡', 2, true, NOW()),
('22222222-2222-2222-2222-222222222239', '11111111-1111-1111-1111-111111111110', 'Mécanicien industriel', 'MECA_IND', 'Maintenance mécanique machines', '#60A5FA', '⚙️', 3, true, NOW()),
('22222222-2222-2222-2222-222222222240', '11111111-1111-1111-1111-111111111110', 'Automaticien', 'AUTOM', 'Maintenance systèmes automatisés', '#93C5FD', '🤖', 4, true, NOW());

-- Verify counts
SELECT 'Predefined Sectors: ' || COUNT(*) as message FROM "PredefinedSectors";
SELECT 'Predefined Industries: ' || COUNT(*) as message FROM "PredefinedIndustries";
