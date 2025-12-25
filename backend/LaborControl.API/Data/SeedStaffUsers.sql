-- Script de seed pour les utilisateurs staff Labor Control
-- À exécuter manuellement après la migration

-- Créer les comptes staff initiaux
INSERT INTO "StaffUsers" ("Id", "Email", "Nom", "Prenom", "PasswordHash", "Role", "Department", "RequiresPasswordChange", "CreatedAt", "IsActive")
VALUES
    -- SuperAdmin (vous)
    ('550e8400-e29b-41d4-a716-446655440001', 'superadmin@labor-control.fr', 'PASTOR', 'Jean-Claude', '$2a$10$provisoire', 'SUPERADMIN', 'Direction', true, NOW(), true),

    -- Aimée PASTOR - ADMIN_STAFF
    ('550e8400-e29b-41d4-a716-446655440002', 'aimee.pastor@labor-control.fr', 'PASTOR', 'Aimée', '$2a$10$provisoire', 'ADMIN_STAFF', 'Gestion', true, NOW(), true),

    -- Codjo TINHAN - ADMIN_STAFF
    ('550e8400-e29b-41d4-a716-446655440003', 'codjo.tinhan@labor-control.fr', 'TINHAN', 'Codjo', '$2a$10$provisoire', 'ADMIN_STAFF', 'Opérations', true, NOW(), true);

-- Note: Les mots de passe provisoires doivent être remplacés par des vrais hachés BCrypt
-- Utilisez le endpoint POST /api/staff-auth/create-staff pour créer d'autres comptes
