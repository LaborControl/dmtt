# LABOR CONTROL DMTT - Analyse Architecture

Date : 24 décembre 2025
Projet : Fork de LABOR CONTROL pour le marché du démantèlement nucléaire (Tricastin)

## Structure du Projet

```
labor-control-dmtt/
├── backend/              # API .NET Core 9.0
├── mobile/               # App React Native (Expo)
├── web-dashboard/        # Dashboard Blazor (repo vide actuellement)
├── shared/               # Librairies communes .NET
└── docs/                 # Documentation
```

## 1. Backend (.NET Core 9.0)

### Technologies
- **Framework** : .NET 9.0
- **Base de données** : PostgreSQL (Npgsql.EntityFrameworkCore.PostgreSQL 9.0.2)
- **ORM** : Entity Framework Core 9.0
- **Authentification** : JWT (Microsoft.AspNetCore.Authentication.JwtBearer)
- **Sécurité** : BCrypt.Net-Next 4.0.3
- **Azure** : Azure.Communication.Email, Azure Key Vault
- **PDF** : QuestPDF 2024.10.3
- **Excel** : EPPlus 7.0.0
- **NFC/RFID** : PCSC 7.0.1, PCSC.Iso7816 7.0.1
- **Rate Limiting** : AspNetCoreRateLimit 5.0.0
- **Paiements** : Stripe.net 49.0.0

### Modèles Existants (47 entités)

#### Gestion Équipements & Assets
- `Asset` : Équipements/Actifs
- `EquipmentCategory` : Catégories d'équipements
- `EquipmentType` : Types d'équipements
- `EquipmentStatus` : Statuts d'équipements
- `PredefinedEquipmentCategory`, `PredefinedEquipmentType` : Données prédéfinies

#### Gestion Tâches & Maintenance
- `Task` : Tâches
- `TaskExecution` : Exécution des tâches
- `TaskDeviation` : Déviations/anomalies
- `TaskTemplate` : Modèles de tâches
- `TaskTemplateQualification` : Qualifications requises par template
- `MaintenanceSchedule` : Planification maintenance
- `MaintenanceExecution` : Exécution maintenance
- `MaintenanceTask` : Tâches de maintenance
- `MaintenanceScheduleQualification` : Qualifications requises
- `ScheduledTask` : Tâches planifiées

#### Gestion Utilisateurs & Équipes
- `User` : Utilisateurs
- `StaffUser` : Personnel staff
- `Team` : Équipes
- `TeamSite` : Association équipe-site
- `UserQualification` : Qualifications des utilisateurs
- `Qualification` : Qualifications
- `PredefinedQualification` : Qualifications prédéfinies

#### Gestion NFC/RFID
- `RfidChip` : Puces RFID/NFC
- `RfidChipStatus` : Statuts des puces
- `RfidChipStatusHistory` : Historique des statuts

#### Gestion Sites & Zones
- `Site` : Sites/Lieux
- `Zone` : Zones dans les sites
- `ControlPoint` : Points de contrôle

#### Gestion Clients & Fournisseurs
- `Customer` : Clients
- `Supplier` : Fournisseurs
- `SupplierOrder` : Commandes fournisseurs
- `SupplierOrderLine` : Lignes de commandes

#### Commerce & Commandes
- `Product` : Produits
- `Order` : Commandes
- `CartItem` : Panier
- `BoxtalShipment` : Expéditions Boxtal

#### Référentiels Métiers
- `Industry` : Industries/Secteurs
- `PredefinedIndustry` : Industries prédéfinies
- `Sector` : Secteurs
- `PredefinedSector` : Secteurs prédéfinis
- `FavoriteManufacturer` : Fabricants favoris

#### Système
- `ErrorLog` : Logs d'erreurs
- `EmailMessage` : Messages email
- `PasswordResetToken` : Tokens de reset password
- `HomeContent` : Contenu page d'accueil

### Contrôleurs Existants (48 endpoints)

Les contrôleurs suivants sont implémentés :
- Authentication (Auth, StaffAuth, Password Reset)
- Assets, Equipment (Categories, Types, Status)
- Tasks (Tasks, TaskTemplates, TaskExecutions)
- Maintenance (Schedules, Executions)
- RFID/NFC (RfidChips, RfidReader, StaffRfid)
- Users, Qualifications, UserQualifications
- Teams, Sites, Zones, ControlPoints
- Customers, Suppliers, SupplierOrders
- Orders, Cart, Products, Payments, Shipments
- Industries, Sectors, Predefined data
- Diagnostics, Errors, Test, SeedData

### Structure Backend
```
LaborControl.API/
├── Controllers/         # 48 contrôleurs API REST
├── Models/              # 47 modèles de données
├── Data/                # DbContext et migrations
├── DTOs/                # Data Transfer Objects
├── Services/            # Services métier
├── Middleware/          # Middlewares (auth, logging, etc.)
├── Migrations/          # Migrations EF Core
├── wwwroot/             # Fichiers statiques
└── Program.cs           # Configuration et startup
```

## 2. Mobile (React Native + Expo)

### Technologies
- **Framework** : React Native 0.81.5
- **Navigation** : Expo Router 6.0, React Navigation 7
- **State Management** : Zustand 5.0.8
- **Storage** :
  - AsyncStorage 2.2.0
  - MMKV 4.0.1 (fast key-value storage)
  - Keychain 10.0.0 (secure storage)
- **NFC** : react-native-nfc-manager 3.17.1
- **Biométrie** : react-native-biometrics 3.0.1
- **Offline** : NetInfo 11.4.1
- **Images** : expo-image, expo-image-picker
- **UI** :
  - Expo Symbols 1.0.7
  - Reanimated 4.1.1
  - Gesture Handler 2.28.0

### Structure Mobile
```
mobile/
├── app/                  # Routes Expo Router
│   ├── (auth)/          # Écrans authentification
│   ├── (user)/          # Écrans utilisateur
│   ├── (admin)/         # Écrans admin
│   └── (supervisor)/    # Écrans superviseur
├── components/          # Composants réutilisables
├── store/               # State management Zustand
│   ├── taskStore.ts
│   ├── anomalyStore.ts
│   └── offlineQueue.ts  # Gestion offline
├── services/            # Services API
├── contexts/            # React Contexts
├── hooks/               # Custom hooks
├── utils/               # Utilitaires
└── constants/           # Constantes
```

### Architecture Mobile
- **Routing** : Expo Router avec groupes de routes par rôle
- **Offline-first** : Queue de synchronisation (offlineQueue.ts)
- **NFC** : Gestion lecture/écriture tags NFC
- **Stores Zustand** :
  - taskStore : Gestion des tâches
  - anomalyStore : Gestion des anomalies
  - offlineQueue : File d'attente sync

**Note importante** : Le repo mobile ne mentionne PAS Watermelon DB contrairement aux specs initiales. L'app utilise AsyncStorage + MMKV pour le stockage.

## 3. Shared (.NET 9.0)

### Technologies
- **SDK** : Microsoft.NET.Sdk.Razor
- **Framework** : .NET 9.0
- **Blazor** : Microsoft.AspNetCore.Components.Web 9.0.9
- **Storage** : Blazored.SessionStorage 2.4.0

### Structure Shared
```
LaborControl.Shared/
├── Components/          # Composants Blazor réutilisables
├── Models/              # Modèles partagés
├── Services/            # Services partagés
└── Class1.cs            # Fichier de base
```

**Package NuGet** :
- ID: LaborControl.Shared
- Version: 1.0.0
- Build automatique du package

## 4. Web Dashboard (Blazor)

**Statut** : Repository vide actuellement
**Prévu** : Dashboard Blazor Server ou WebAssembly

## Points Clés pour Adaptation DMTT

### 1. Éléments Réutilisables

#### Backend
- Architecture .NET Core 9.0 solide
- Système d'authentification JWT multi-rôles
- Gestion NFC/RFID déjà implémentée
- Système de qualifications (à adapter)
- Gestion sites/zones (réutilisable)
- Système de tasks/execution (base pour soudures/contrôles)
- PostgreSQL + EF Core (migrations)

#### Mobile
- Architecture Expo Router par rôles
- Gestion NFC fonctionnelle
- Système offline-first
- Multi-profils utilisateurs

### 2. Nouvelles Entités Requises

#### Métier Nucléaire
- `Weld` (Soudure) : repère, diamètre, épaisseur, matériaux, procédé, classe
- `Material` : matériaux avec CCPU
- `DMOS` : Procédures de soudage
- `NDTControl` : Contrôles non destructifs (VT, PT, MT, RT, UT)
- `NDTProgram` : Programmes de CND
- `NonConformity` (FNC) : Fiches de non-conformité
- `WelderQualification` : Qualifications soudeurs
- `NDTControllerQualification` : Qualifications contrôleurs
- `TechnicalDocument` : Plans BE, CDC, certificats, normes
- `EquipmentWeld` : Association équipement-soudures
- `ComplianceCertificate` : Certificats de conformité

#### Workflows
- `CCPUValidation` : Validations CCPU (matériaux, soudures)
- `WeldingCoordinatorApproval` : Validations coordinateur soudage
- `QualityInspection` : Inspections qualité
- `EDFInspection` : Validations inspecteur EDF
- `WorkflowStep` : Étapes des workflows avec verrouillage

### 3. Modifications Contrôleurs

#### Nouveaux Contrôleurs
- `WeldsController` : CRUD soudures
- `MaterialsController` : CRUD matériaux + validation CCPU
- `NDTControlsController` : Saisie/validation contrôles CND
- `NDTProgramsController` : Génération/gestion programmes CND
- `NonConformitiesController` : Gestion FNC
- `TechnicalDocumentsController` : Upload/gestion documents
- `CCPUValidationsController` : Workflow validation CCPU
- `WelderQualificationsController` : Gestion qualifications soudeurs
- `AIProcedureGeneratorController` : Génération procédures par IA
- `PlanningController` : Gantt + gestion ressources

#### Adaptations Contrôleurs Existants
- `QualificationsController` : Adapter pour soudeurs/contrôleurs CND
- `TasksController` : Adapter pour opérations de soudage/contrôle
- `TeamsController` : Adapter pour équipes nucléaires
- `RfidChipsController` : Adapter pour équipements nucléaires

### 4. Agents IA à Développer

Ces agents doivent être intégrés au backend :

1. **PreValidationQualificationAgent**
   - Endpoint : `/api/ai/validate-qualification`
   - Analyse documents PDF/images de qualifications
   - Extraction données : nom, numéro, date expiration, procédés
   - Retour : JSON structuré + niveau confiance

2. **NDTProgramGeneratorAgent**
   - Endpoint : `/api/ai/generate-ndt-program`
   - Input : caractéristiques soudure + normes EDF + CDC
   - Génération programme CND automatique
   - Retour : PDF + données structurées

3. **ProcedureGeneratorAgent**
   - Endpoint : `/api/ai/generate-procedure`
   - Input : CDC ORANO + normes + type opération
   - Génération procédures spécifiques projet
   - Retour : PDF + checklist

4. **NDTAdaptationAgent**
   - Endpoint : `/api/ai/adapt-ndt-program`
   - Input : FNC + programme CND initial
   - Adaptation programme suite non-conformité
   - Retour : Programme adapté

5. **PlanningAgent**
   - Endpoint : `/api/ai/generate-planning`
   - Input : soudures + ressources + contraintes
   - Génération Gantt avec dépendances
   - Retour : Planning optimisé JSON

### 5. Profils Utilisateurs

#### Nouveaux Rôles à Ajouter
1. `Subcontractor` : Sous-traitant (dépôt docs)
2. `Welder` : Soudeur (saisie exécution)
3. `NDTController` : Contrôleur CND (saisie contrôles)
4. `CCPU` : Validation matériaux/soudures
5. `WeldingCoordinator` : Coordinateur soudage (validation qualifs)
6. `QualityManager` : Responsable qualité (validation FNC, programmes)
7. `EDFInspector` : Inspecteur EDF (validation finale)
8. `Planner` : Planificateur (gestion Gantt)

### 6. Système NFC Adapté

#### Modification Approche
- **Actuel** : NFC par asset
- **DMTT** : NFC par équipement/partie d'équipement
  - Tag NFC → Liste soudures associées
  - Sélection soudure → Saisie contrôle
  - Fallback saisie manuelle

#### Modèle Données
```csharp
public class EquipmentNFC
{
    public int Id { get; set; }
    public string NFCTag { get; set; }
    public int EquipmentId { get; set; }
    public Equipment Equipment { get; set; }
    public ICollection<Weld> Welds { get; set; }
}
```

### 7. Documents & Traçabilité

#### Stockage
- **Azure Blob Storage** : PDF, DWG, images
- **Base PostgreSQL** : Métadonnées + références

#### Types Documents
**Entrée** :
- Plans BE (PDF/DWG)
- CDC ORANO
- Certificats matériaux
- Qualifications
- DMOS
- Normes EDF

**Sortie** :
- Programmes CND (générés IA)
- Rapports contrôle
- Dossiers fabrication
- Certificats conformité
- Procédures projet

### 8. Tableaux de Bord

#### Différenciation par Profil
- **Soudeur** : Ses tâches, planning
- **Contrôleur** : Contrôles à réaliser
- **CCPU** : Validations en attente
- **Qualité** : KPI conformité, FNC
- **Planificateur** : Gantt, ressources, retards
- **Inspecteur EDF** : Vue globale, validations

#### KPI Généraux
- Avancement soudures (prévues/réalisées/validées)
- Taux de conformité (%)
- FNC en cours / résolues
- Retards planning
- Ressources disponibles/utilisées

## Prochaines Étapes Recommandées

### Phase 1 : Préparation Base (3 jours)
1. Créer les migrations pour nouvelles entités nucléaires
2. Adapter les DTOs et modèles
3. Créer les nouveaux contrôleurs de base (CRUD)
4. Implémenter système multi-rôles étendu

### Phase 2 : Workflows Métier (4 jours)
1. Implémenter workflows de verrouillage (CCPU, validations)
2. Créer système de traçabilité soudures
3. Adapter système NFC pour équipements
4. Développer gestion documents techniques

### Phase 3 : Agents IA (3 jours)
1. Implémenter PreValidationQualificationAgent
2. Implémenter NDTProgramGeneratorAgent
3. Implémenter ProcedureGeneratorAgent
4. Intégrer agents au backend

### Phase 4 : Mobile & Dashboard (2 jours)
1. Adapter app mobile pour nouveaux profils
2. Créer écrans de saisie soudure/contrôle
3. Implémenter workflow mobile
4. Développer dashboard Blazor de base

## Infrastructure Azure

### Services Requis
- **App Service** : Backend .NET
- **PostgreSQL** : Base de données
- **Blob Storage** : Documents PDF/DWG
- **Key Vault** : Secrets et configuration
- **AI Services** : Pour agents IA (Azure OpenAI)
- **Application Insights** : Monitoring

### Configuration
Tous les secrets dans Azure Key Vault (connection strings, API keys, etc.)

## Conclusion

L'architecture actuelle de LABOR CONTROL fournit une base solide :
- Backend .NET modulaire et extensible
- Système NFC/RFID fonctionnel
- Gestion multi-rôles
- App mobile offline-first

Les principales adaptations requises :
- Nouvelles entités métier nucléaire (soudures, contrôles, matériaux)
- Workflows de verrouillage CCPU/validations
- Agents IA pour génération procédures/programmes
- Adaptation NFC pour équipements
- Nouveaux profils utilisateurs

**Deadline MVP : 12 janvier 2025**
**Temps restant : 19 jours**
