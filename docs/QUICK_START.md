# Quick Start - LABOR CONTROL DMTT

Guide de démarrage rapide pour le développement du projet.

## Prérequis

### Outils Requis
- [ ] **.NET 9.0 SDK** - [Télécharger](https://dotnet.microsoft.com/download/dotnet/9.0)
- [ ] **Node.js 18+** - [Télécharger](https://nodejs.org/)
- [ ] **PostgreSQL 15+** - [Télécharger](https://www.postgresql.org/download/)
- [ ] **Git** - [Télécharger](https://git-scm.com/)
- [ ] **Visual Studio Code** ou **Visual Studio 2022**
- [ ] **Expo CLI** - `npm install -g expo-cli`

### Clés API Requises
- [ ] **Claude API Key** (Anthropic) - [Obtenir](https://console.anthropic.com/)
- [ ] **Gemini API Key** (Google AI) - [Obtenir](https://makersuite.google.com/app/apikey)
- [ ] **Azure Account** (pour Blob Storage)

## Étape 1 : Clone et Setup

Le projet est déjà cloné dans `C:\Users\jcpas\labor-control-dmtt\`

```bash
cd labor-control-dmtt
```

Structure actuelle :
```
labor-control-dmtt/
├── backend/              ✅ Cloné
├── mobile/               ✅ Cloné
├── shared/               ✅ Cloné
├── web-dashboard/        ✅ Cloné (vide)
└── docs/                 ✅ Documentation créée
```

## Étape 2 : Configuration Backend

### 2.1 PostgreSQL Database

```bash
# Créer la base de données
psql -U postgres
CREATE DATABASE laborcontrol_dmtt;
CREATE USER laborcontrol_user WITH PASSWORD 'votre_mot_de_passe';
GRANT ALL PRIVILEGES ON DATABASE laborcontrol_dmtt TO laborcontrol_user;
\q
```

### 2.2 Configuration appsettings.Development.json

```bash
cd backend/LaborControl.API
```

Créer le fichier `appsettings.Development.json` :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=laborcontrol_dmtt;Username=laborcontrol_user;Password=votre_mot_de_passe"
  },
  "JwtSettings": {
    "SecretKey": "votre-secret-key-min-32-caracteres-tres-secret",
    "Issuer": "LABORCONTROL-DMTT",
    "Audience": "LABORCONTROL-DMTT-API",
    "ExpiryInMinutes": 1440
  },
  "Claude": {
    "ApiKey": "sk-ant-api03-...",
    "Model": "claude-3-5-sonnet-20241022",
    "MaxTokens": 4096,
    "Temperature": 0.3
  },
  "Gemini": {
    "ApiKey": "AIza...",
    "Model": "gemini-2.0-flash-exp"
  },
  "AzureBlobStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net",
    "ContainerName": "documents"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### 2.3 Installer les packages

```bash
cd backend/LaborControl.API

# Restore packages
dotnet restore

# Ajouter package Anthropic (Claude)
dotnet add package Anthropic.SDK --version 0.2.0
```

### 2.4 Migrations et Database

```bash
# Appliquer les migrations existantes
dotnet ef database update

# Plus tard, créer les nouvelles migrations (Sprint 1)
dotnet ef migrations add AddNuclearEntities
```

### 2.5 Lancer le backend

```bash
dotnet run
```

L'API devrait être accessible sur : `https://localhost:5001` ou `http://localhost:5000`

Swagger UI : `https://localhost:5001/swagger`

## Étape 3 : Configuration Mobile

### 3.1 Installer les dépendances

```bash
cd ../../mobile

# Install packages
npm install
```

### 3.2 Créer le fichier .env

```bash
# Créer .env à la racine du dossier mobile
```

Contenu de `.env` :
```
API_URL=http://localhost:5000/api
ENVIRONMENT=development
```

**Note** : Si vous testez sur un device physique, remplacez `localhost` par l'IP de votre machine (ex: `http://192.168.1.100:5000/api`)

### 3.3 Lancer l'app mobile

```bash
# Démarrer Expo
npx expo start

# Options :
# - Scan QR code avec Expo Go (iOS/Android)
# - Appuyer sur 'a' pour Android emulator
# - Appuyer sur 'i' pour iOS simulator (Mac uniquement)
# - Appuyer sur 'w' pour web
```

## Étape 4 : Obtenir les Clés API

### Claude (Anthropic)

1. Aller sur https://console.anthropic.com/
2. Créer un compte (ou se connecter)
3. Aller dans "API Keys"
4. Créer une nouvelle clé
5. **Budget** : Commencer avec 10$ de crédit
6. Copier la clé (format : `sk-ant-api03-...`)

### Gemini (Google AI)

1. Aller sur https://makersuite.google.com/app/apikey
2. Se connecter avec compte Google
3. Cliquer "Create API Key"
4. Sélectionner un projet GCP (ou créer nouveau)
5. Copier la clé (format : `AIza...`)

**Note** : Gemini 2.0 Flash est **GRATUIT** jusqu'à 1500 requêtes/jour !

### Azure Blob Storage (optionnel pour MVP local)

Pour le développement local, vous pouvez utiliser **Azurite** (émulateur Azure Storage) :

```bash
npm install -g azurite

# Lancer Azurite
azurite --silent --location c:\azurite --debug c:\azurite\debug.log
```

Connection string pour Azurite :
```
DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;
```

## Étape 5 : Vérification du Setup

### Test Backend

```bash
# Test simple
curl http://localhost:5000/api/test
```

Ou ouvrir Swagger : `https://localhost:5001/swagger`

### Test Mobile

1. L'app Expo devrait s'ouvrir
2. Vous devriez voir l'écran de login
3. Pas encore de compte utilisateur (sera créé au Sprint 1)

### Test IA Services (après implémentation Sprint 3)

```bash
# Test Claude
curl -X POST http://localhost:5000/api/ai/test-claude \
  -H "Content-Type: application/json" \
  -d '{"prompt": "Bonjour"}'

# Test Gemini (avec image)
curl -X POST http://localhost:5000/api/ai/test-gemini \
  -F "file=@test-image.jpg"
```

## Étape 6 : Démarrer le Développement

### Sprint 1 : Modèles de Données (Commencer maintenant !)

Voir `docs/MVP_IMPLEMENTATION_PLAN.md` section "Sprint 1"

**Première tâche** : Créer les 9 nouvelles entités nucléaires

Fichiers à créer dans `backend/LaborControl.API/Models/` :
1. `Weld.cs`
2. `Material.cs`
3. `DMOS.cs`
4. `NDTControl.cs`
5. `NDTProgram.cs`
6. `NonConformity.cs`
7. `WelderQualification.cs`
8. `TechnicalDocument.cs`
9. `Equipment.cs` (extension de Asset)

Le code complet est dans le plan MVP !

## Structure de Développement Recommandée

### Workflow Git (à créer)

```bash
# Créer un nouveau repo Git pour DMTT
cd labor-control-dmtt
git init
git add .
git commit -m "Initial setup - fork LABOR CONTROL DMTT"

# Créer repo sur GitHub et pusher
git remote add origin https://github.com/votre-org/labor-control-dmtt.git
git push -u origin main
```

### Branches

```bash
# Créer une branche pour Sprint 1
git checkout -b sprint-1/nuclear-entities

# Après complétion
git checkout main
git merge sprint-1/nuclear-entities
```

## Commandes Utiles

### Backend

```bash
# Build
dotnet build

# Run avec hot reload
dotnet watch run

# Créer migration
dotnet ef migrations add MigrationName

# Appliquer migrations
dotnet ef database update

# Rollback migration
dotnet ef database update PreviousMigrationName

# Supprimer dernière migration
dotnet ef migrations remove
```

### Mobile

```bash
# Démarrer
npx expo start

# Clear cache
npx expo start -c

# Build Android (pour test)
npx expo run:android

# Build iOS (Mac uniquement)
npx expo run:ios
```

### Database

```bash
# Se connecter à PostgreSQL
psql -U laborcontrol_user -d laborcontrol_dmtt

# Lister les tables
\dt

# Voir structure table
\d table_name

# Exécuter script SQL
\i script.sql
```

## Troubleshooting

### Erreur : "Cannot connect to PostgreSQL"

```bash
# Vérifier que PostgreSQL est lancé
# Windows :
sc query postgresql-x64-15

# Démarrer si nécessaire
net start postgresql-x64-15
```

### Erreur : "Claude API Key invalid"

- Vérifier que la clé commence par `sk-ant-api03-`
- Vérifier qu'elle est bien dans `appsettings.Development.json`
- Vérifier que vous avez du crédit sur votre compte Anthropic

### Erreur : "Gemini API rate limit"

- Gemini Flash est limité à 1500 req/jour gratuit
- Vérifier dans https://makersuite.google.com/app/apikey

### Erreur Mobile : "Network request failed"

- Vérifier que le backend est lancé
- Vérifier l'URL dans `.env`
- Si sur device physique, utiliser l'IP locale (pas localhost)
- Vérifier le firewall Windows

## Prochaines Étapes

1. ✅ Setup complet (vous y êtes !)
2. 📝 Lire la documentation complète :
   - `docs/ARCHITECTURE_ANALYSIS.md`
   - `docs/MVP_IMPLEMENTATION_PLAN.md`
   - `docs/AI_ARCHITECTURE.md`
3. 🚀 Commencer Sprint 1 : Créer les modèles de données
4. 🧪 Tester les migrations
5. 🔄 Créer les contrôleurs CRUD (Sprint 2)

## Ressources

### Documentation Projet
- Architecture Analysis : `docs/ARCHITECTURE_ANALYSIS.md`
- Plan MVP : `docs/MVP_IMPLEMENTATION_PLAN.md`
- Architecture IA : `docs/AI_ARCHITECTURE.md`
- Code Services IA : `docs/AI_SERVICES_CODE.md`

### APIs Utilisées
- Claude API : https://docs.anthropic.com/
- Gemini API : https://ai.google.dev/docs
- .NET 9 : https://learn.microsoft.com/en-us/dotnet/
- React Native : https://reactnative.dev/
- Expo : https://docs.expo.dev/

### Normes Nucléaires (Référence)
- RCC-M : Règles de Conception et Construction
- RSEM : Règles de Surveillance en Exploitation
- COFREND : Confédération Française pour les Essais Non Destructifs

## Support

Pour toute question :
1. Consulter la documentation dans `/docs`
2. Vérifier les logs backend : `backend/LaborControl.API/logs/`
3. Vérifier les logs mobile : Console Expo

---

**Bonne chance pour le développement ! Deadline MVP : 12 janvier 2025** 🚀
