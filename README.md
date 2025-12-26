# LABOR CONTROL DMTT

Fork complet de LABOR CONTROL adapté au marché du démantèlement de site nucléaire

## Projet

### Contexte
Système de traçabilité et contrôle qualité pour les opérations de démantèlement d'installation nucléaire. Le système couvre la gestion complète des soudures, contrôles non destructifs (CND), validations CCPU, et génération automatique de procédures et programmes de contrôle par IA.

## Structure du Projet

```
labor-control-dmtt/
├── backend/              # API .NET Core 9.0 + PostgreSQL
├── mobile/               # App React Native (Expo) - Offline-first
├── web-dashboard/        # Dashboard Blazor (à développer)
├── shared/               # Librairies communes .NET
└── docs/                 # Documentation
    ├── ARCHITECTURE_ANALYSIS.md        # Analyse architecture existante
    └── MVP_IMPLEMENTATION_PLAN.md      # Plan détaillé MVP
```

## Technologies

### Backend
- **.NET Core 9.0** - Framework principal
- **PostgreSQL** - Base de données
- **Entity Framework Core 9.0** - ORM
- **Claude (Anthropic)** - Génération procédures et programmes CND
- **Gemini (Google)** - Extraction données depuis PDF/images
- **Azure Blob Storage** - Stockage documents (PDF, DWG, certificats)
- **QuestPDF** - Génération PDF
- **JWT** - Authentification

### Mobile
- **React Native 0.81.5** - Framework mobile
- **Expo Router 6.0** - Navigation
- **Zustand 5.0** - State management
- **React Native NFC Manager** - Lecture tags NFC
- **AsyncStorage + MMKV** - Stockage local
- **Offline-first** - Synchronisation différée

### Infrastructure
- **Azure App Service** - Hébergement backend
- **Azure PostgreSQL** - Base de données cloud
- **Azure Blob Storage** - Documents
- **Claude API** - Génération structurée (Anthropic)
- **Gemini API** - Vision & OCR (Google AI)
- **Azure Key Vault** - Secrets

## Fonctionnalités MVP

### 1. Gestion Soudures
- Création et traçabilité des soudures (repère, diamètre, épaisseur, matériaux)
- Association soudures ↔ équipements
- Exécution par soudeurs qualifiés (NFC mobile)
- Validation CCPU

### 2. Contrôles Non Destructifs (CND)
- Génération automatique programmes CND par IA
- Saisie résultats contrôles (VT, PT, MT, RT, UT)
- Upload rapports et photos
- Validation par CCPU

### 3. Gestion Qualifications
- Qualifications soudeurs (TIG, MIG, matériaux, diamètres)
- Qualifications contrôleurs CND
- Pré-validation automatique par IA (extraction données certificats)
- Validation coordinateur soudage

### 4. Matériaux
- Réception matériaux avec certificats
- Validation CCPU avant débit
- Traçabilité matériaux → soudures

### 5. Procédures
- Génération automatique procédures par IA (CDC + normes EDF)
- DMOS (Descriptifs de Mode Opératoire de Soudage)
- Programmes de CND personnalisés

### 6. Workflows de Verrouillage
1. Matériau → Validation CCPU → Débit autorisé
2. Qualification soudeur → Validation coordinateur → Soudage autorisé
3. Soudure exécutée → Contrôle CND autorisé
4. CND validé → CCPU valide → Étape suivante

### 7. Mobile Offline-first
- Scan NFC équipement → Liste soudures
- Saisie exécution soudure (soudeur)
- Saisie résultat contrôle (contrôleur)
- Validation matériaux (CCPU)
- Synchronisation automatique

### 8. Agents IA (3 agents MVP)

**Architecture Multi-IA** : Claude (Anthropic) + Gemini (Google)

#### Agent 1 : Pré-validation Qualifications (Gemini 2.0 Flash)
- Analyse certificats PDF/images avec vision multimodale
- OCR et extraction données structurées
- Détection anomalies/expirations
- Coût : GRATUIT (sous limite 1500 req/jour)

#### Agent 2 : Génération Programmes CND (Claude 3.5 Sonnet)
- Input : caractéristiques soudures + normes + CDC
- Raisonnement complexe selon RCC-M et normes EDF
- Génération programme CND conforme
- Output : PDF + données structurées JSON

#### Agent 3 : Génération Procédures (Claude 3.5 Sonnet)
- Input : CDC ORANO + normes EDF + type opération
- Génération procédures détaillées opérationnelles
- Contexte long (200k tokens) pour analyse CDC complète
- Output : PDF avec checklist et étapes numérotées

**Coût estimé** : ~25$/mois (très économique)
Voir `docs/AI_ARCHITECTURE.md` pour détails complets.

## Profils Utilisateurs

1. **Sous-traitant** : Dépôt documents techniques et certificats
2. **Soudeur** : Exécution soudures (mobile NFC)
3. **Contrôleur CND** : Saisie résultats contrôles (mobile NFC)
4. **CCPU** : Validation matériaux et soudures
5. **Coordinateur Soudage** : Validation qualifications soudeurs
6. **Responsable Qualité** : Gestion FNC, validation programmes
7. **Inspecteur EDF** : Validation finale (Phase 2)
8. **Planificateur** : Gestion planning Gantt (Phase 2)

## Installation & Setup

### Prérequis
- .NET 9.0 SDK
- Node.js 18+
- PostgreSQL 15+
- Azure CLI
- Expo CLI
- Git

### Backend
```bash
cd labor-control-dmtt/backend/LaborControl.API

# Restore packages
dotnet restore

# Configuration (créer appsettings.Development.json)
# Voir MVP_IMPLEMENTATION_PLAN.md section "Configuration Environnement"

# Migrations
dotnet ef database update

# Run
dotnet run
```

### Mobile
```bash
cd labor-control-dmtt/mobile

# Install
npm install

# Configuration (.env)
API_URL=http://localhost:5000/api

# Run
npx expo start
```

## Roadmap

### Sprint 1 : Fondations Backend (25-28 déc)
- Création 9 nouvelles entités nucléaires
- Migrations PostgreSQL
- Extension modèle User (nouveaux rôles)

### Sprint 2 : Contrôleurs & API (29 déc - 1er jan)
- 8 nouveaux contrôleurs CRUD
- Endpoints spécialisés (validation, upload, etc.)
- Authentification JWT multi-rôles

### Sprint 3 : Agents IA & Services (2-5 jan)
- Azure Blob Storage integration
- Azure OpenAI integration
- 3 agents IA fonctionnels

### Sprint 4 : Mobile App (6-9 jan)
- Écrans soudeur/contrôleur/CCPU
- NFC équipements → soudures
- Offline-first avec queue sync

### Sprint 5 : Workflows & Finitions (10-12 jan)
- Workflows de verrouillage
- Notifications
- Tests end-to-end
- Déploiement Azure
- **DEMO MVP**

## Phase 2 (Post-12 janvier)
- Planning Gantt automatique (Agent IA)
- Tableaux de bord avancés (Power BI)
- Module inspecteur EDF
- Dashboard Blazor web
- Export dossiers fabrication complets
- Reporting conformité avancé

## Documentation

- **ARCHITECTURE_ANALYSIS.md** : Analyse détaillée de l'architecture existante de LABOR CONTROL et adaptations requises pour le nucléaire
- **MVP_IMPLEMENTATION_PLAN.md** : Plan d'implémentation détaillé sur 5 sprints avec tous les détails techniques
- **AI_ARCHITECTURE.md** : Architecture multi-IA (Claude + Gemini), stratégie de répartition, prompts détaillés
- **AI_SERVICES_CODE.md** : Code complet des services IA (ClaudeService, GeminiService, agents)

## Spécificités Métier Nucléaire

### Normes & Références
- **RCC-M** : Règles de Conception et Construction des Matériels mécaniques
- **EN ISO 17640** : Contrôles par ultrasons des assemblages soudés
- **EN ISO 17637** : Contrôles visuels
- **EN ISO 3452** : Contrôles par ressuage
- **Normes EDF** : Stockées sur serveur pour accès agents IA

### Contrôles Non Destructifs (CND)
- **VT** : Visual Testing (Contrôle visuel)
- **PT** : Penetrant Testing (Ressuage)
- **MT** : Magnetic Testing (Magnétoscopie)
- **RT** : Radiographic Testing (Radiographie)
- **UT** : Ultrasonic Testing (Ultrasons)

### Documents Techniques
- **CDC** : Cahier des Charges (ORANO)
- **DMOS** : Descriptif de Mode Opératoire de Soudage
- **CCPU** : Contrôle Conformité Produits Utilisés
- **FNC** : Fiche de Non-Conformité

## Déploiement

### Azure Resources
```bash
# Resource Group
az group create --name rg-laborcontrol-dmtt --location francecentral

# App Service Plan
az appservice plan create --name plan-laborcontrol-dmtt --resource-group rg-laborcontrol-dmtt --sku B1 --is-linux

# Web App
az webapp create --name app-laborcontrol-dmtt --resource-group rg-laborcontrol-dmtt --plan plan-laborcontrol-dmtt --runtime "DOTNET|9.0"

# PostgreSQL
az postgres flexible-server create --name psql-laborcontrol-dmtt --resource-group rg-laborcontrol-dmtt --location francecentral --admin-user adminuser --admin-password <password> --sku-name Standard_B1ms --version 15

# Blob Storage
az storage account create --name stlaborcontroldmtt --resource-group rg-laborcontrol-dmtt --location francecentral --sku Standard_LRS

# Key Vault
az keyvault create --name kv-laborcontrol-dmtt --resource-group rg-laborcontrol-dmtt --location francecentral
```

Voir documentation détaillée dans `docs/MVP_IMPLEMENTATION_PLAN.md` section "Déploiement Azure".

## Sécurité

- **JWT** : Authentification stateless
- **BCrypt** : Hash passwords
- **Azure Key Vault** : Secrets centralisés
- **HTTPS** : Encryption transport
- **RBAC** : Autorisation par rôle
- **Rate Limiting** : Protection DDoS

## Support

Pour toute question technique ou fonctionnelle, consulter la documentation dans `/docs`.

## Licence

Propriétaire - LABOR CONTROL DMTT © 2025

