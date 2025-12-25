-- Script pour appliquer toutes les migrations en attente
-- À exécuter manuellement sur la base de données PostgreSQL

-- Note: Ce script doit être généré via dotnet ef migrations script
-- depuis la dernière migration appliquée

-- Pour l'instant, vérifions juste la connexion et l'état actuel
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 5;
