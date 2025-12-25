# Plan d'Implémentation MVP - LABOR CONTROL DMTT

**Deadline : 12 janvier 2025**
**Temps disponible : 19 jours**
**Objectif : MVP fonctionnel pour démantèlement Tricastin**

## Vue d'Ensemble

### Objectifs MVP
1. ✅ Traçabilité des tâches de contrôle (soudure, CND, CCPU)
2. ✅ Génération automatique des procédures par IA à partir du CDC ORANO
3. ✅ Multi-profils utilisateurs spécifiques nucléaire
4. ✅ Workflows de verrouillage CCPU/validations
5. ✅ NFC sur équipements → soudures
6. ✅ Mobile offline-first pour terrain

### Hors Scope MVP
- Planning Gantt automatique (Phase 2)
- Tableaux de bord avancés (Phase 2)
- Export complet dossiers fabrication (Phase 2)
- Gestion complète des FNC (simplifié en MVP)

## Sprints (5 sprints de 3-4 jours)

---

## Sprint 1 : Fondations Backend (4 jours - 25-28 déc)

### Objectif
Créer la base de données et l'architecture backend pour les entités nucléaires.

### Tasks

#### 1.1 Modèles de Données Nucléaires
**Fichier** : `backend/LaborControl.API/Models/`

Créer les entités suivantes :

```csharp
// Weld.cs
public class Weld
{
    public int Id { get; set; }
    public string Reference { get; set; }              // Repère soudure
    public int EquipmentId { get; set; }
    public Equipment Equipment { get; set; }
    public string Diameter { get; set; }               // DN50, DN100, etc.
    public string Thickness { get; set; }              // Épaisseur en mm
    public string Material1 { get; set; }              // Matériau tube 1
    public string Material2 { get; set; }              // Matériau tube 2
    public string WeldingProcess { get; set; }         // TIG, MIG, etc.
    public string WeldClass { get; set; }              // A, B, C
    public int? DMOSId { get; set; }
    public DMOS? DMOS { get; set; }
    public int? WelderId { get; set; }
    public User? Welder { get; set; }
    public DateTime? ExecutionDate { get; set; }
    public string Status { get; set; }                 // Pending, InProgress, Welded, Controlled, Validated
    public int? CCPUValidatorId { get; set; }
    public User? CCPUValidator { get; set; }
    public DateTime? CCPUValidationDate { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public ICollection<NDTControl> NDTControls { get; set; }
}

// Material.cs
public class Material
{
    public int Id { get; set; }
    public string Reference { get; set; }
    public string Name { get; set; }
    public string Grade { get; set; }                  // Nuance
    public string Supplier { get; set; }
    public string CertificateNumber { get; set; }
    public string CertificateFilePath { get; set; }    // Azure Blob
    public DateTime ReceiptDate { get; set; }
    public string Status { get; set; }                 // PendingCCPU, Approved, Rejected
    public int? CCPUValidatorId { get; set; }
    public User? CCPUValidator { get; set; }
    public DateTime? CCPUValidationDate { get; set; }
    public string? CCPUComments { get; set; }
}

// DMOS.cs (Descriptif de Mode Opératoire de Soudage)
public class DMOS
{
    public int Id { get; set; }
    public string Reference { get; set; }
    public string Name { get; set; }
    public string FilePath { get; set; }               // Azure Blob
    public string WeldingProcess { get; set; }
    public string Status { get; set; }                 // PendingApproval, Approved
    public int? ApprovedById { get; set; }
    public User? ApprovedBy { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public ICollection<Weld> Welds { get; set; }
}

// NDTControl.cs (Contrôle Non Destructif)
public class NDTControl
{
    public int Id { get; set; }
    public int WeldId { get; set; }
    public Weld Weld { get; set; }
    public string ControlType { get; set; }            // VT, PT, MT, RT, UT
    public int? ControllerId { get; set; }
    public User? Controller { get; set; }
    public DateTime? ControlDate { get; set; }
    public string Result { get; set; }                 // Pending, Conform, NonConform
    public string? Comments { get; set; }
    public string? ReportFilePath { get; set; }        // Azure Blob
    public int? NDTProgramId { get; set; }
    public NDTProgram? NDTProgram { get; set; }
    public int? NonConformityId { get; set; }
    public NonConformity? NonConformity { get; set; }
}

// NDTProgram.cs
public class NDTProgram
{
    public int Id { get; set; }
    public string Reference { get; set; }
    public int? EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }
    public string FilePath { get; set; }               // Azure Blob - PDF généré
    public DateTime CreationDate { get; set; }
    public bool GeneratedByAI { get; set; }
    public string? AIModelVersion { get; set; }
    public string Status { get; set; }                 // Draft, Approved
    public int? ApprovedById { get; set; }
    public User? ApprovedBy { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public ICollection<NDTControl> Controls { get; set; }
}

// NonConformity.cs (FNC - Fiche de Non-Conformité)
public class NonConformity
{
    public int Id { get; set; }
    public string Reference { get; set; }
    public int? WeldId { get; set; }
    public Weld? Weld { get; set; }
    public int? NDTControlId { get; set; }
    public NDTControl? NDTControl { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }                 // Open, InTreatment, Closed
    public DateTime CreationDate { get; set; }
    public int CreatedById { get; set; }
    public User CreatedBy { get; set; }
    public string? CorrectiveAction { get; set; }
    public DateTime? ResolutionDate { get; set; }
    public int? ClosedById { get; set; }
    public User? ClosedBy { get; set; }
}

// WelderQualification.cs
public class WelderQualification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public string QualificationNumber { get; set; }
    public string WeldingProcess { get; set; }         // TIG, MIG, etc.
    public string Materials { get; set; }              // Matériaux qualifiés
    public string ThicknessRange { get; set; }         // Ex: "3-10mm"
    public string DiameterRange { get; set; }          // Ex: "DN50-DN200"
    public DateTime IssueDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public string CertificateFilePath { get; set; }    // Azure Blob
    public string Status { get; set; }                 // Valid, Expired, PendingApproval
    public int? ApprovedById { get; set; }
    public User? ApprovedBy { get; set; }              // Coordinateur soudage
    public DateTime? ApprovalDate { get; set; }
}

// TechnicalDocument.cs
public class TechnicalDocument
{
    public int Id { get; set; }
    public string Reference { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }                   // CDC, Plan, Norme, Certificate, DMOS
    public string FilePath { get; set; }               // Azure Blob
    public DateTime UploadDate { get; set; }
    public int UploadedById { get; set; }
    public User UploadedBy { get; set; }
    public int? EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }
    public string? Version { get; set; }
}

// Equipment - Extension du modèle Asset existant
// Ajouter à Asset.cs :
public class Equipment : Asset
{
    public string? EquipmentCode { get; set; }         // Code équipement Tricastin
    public string? System { get; set; }                // Système (circuit auxiliaire, etc.)
    public ICollection<Weld> Welds { get; set; }
    public ICollection<TechnicalDocument> Documents { get; set; }
    public ICollection<NDTProgram> NDTPrograms { get; set; }
}
```

**Livrable** : 9 nouvelles entités + modification Asset

#### 1.2 Extension Modèle User
**Fichier** : `backend/LaborControl.API/Models/User.cs`

Ajouter les rôles nucléaires :
```csharp
public enum UserRole
{
    // Rôles existants
    Admin,
    User,
    Supervisor,

    // Nouveaux rôles DMTT
    Subcontractor,
    Welder,
    NDTController,
    CCPU,
    WeldingCoordinator,
    QualityManager,
    EDFInspector,
    Planner
}
```

#### 1.3 DbContext & Migrations
**Fichier** : `backend/LaborControl.API/Data/ApplicationDbContext.cs`

Ajouter DbSets :
```csharp
public DbSet<Weld> Welds { get; set; }
public DbSet<Material> Materials { get; set; }
public DbSet<DMOS> DMOSs { get; set; }
public DbSet<NDTControl> NDTControls { get; set; }
public DbSet<NDTProgram> NDTPrograms { get; set; }
public DbSet<NonConformity> NonConformities { get; set; }
public DbSet<WelderQualification> WelderQualifications { get; set; }
public DbSet<TechnicalDocument> TechnicalDocuments { get; set; }
```

Créer migration :
```bash
dotnet ef migrations add AddNuclearEntities
dotnet ef database update
```

**Livrable** : DbContext mis à jour + migration créée

#### 1.4 DTOs
**Fichier** : `backend/LaborControl.API/DTOs/`

Créer les DTOs pour chaque entité (Create, Update, Response).

**Livrable** : 24 DTOs (3 par entité × 8 entités)

### Checklist Sprint 1
- [ ] 9 modèles créés et testés
- [ ] User.cs étendu avec nouveaux rôles
- [ ] DbContext mis à jour
- [ ] Migration créée et appliquée
- [ ] 24 DTOs créés
- [ ] Compilation sans erreur

---

## Sprint 2 : Contrôleurs & API (4 jours - 29 déc - 1er jan)

### Objectif
Créer les endpoints API CRUD pour toutes les entités nucléaires.

### Tasks

#### 2.1 WeldsController
**Fichier** : `backend/LaborControl.API/Controllers/WeldsController.cs`

Endpoints :
- `GET /api/welds` : Liste avec filtres
- `GET /api/welds/{id}` : Détail
- `POST /api/welds` : Création
- `PUT /api/welds/{id}` : Mise à jour
- `DELETE /api/welds/{id}` : Suppression
- `GET /api/welds/equipment/{equipmentId}` : Par équipement
- `POST /api/welds/{id}/execute` : Marquer comme exécutée (soudeur)
- `POST /api/welds/{id}/ccpu-validate` : Validation CCPU
- `GET /api/welds/{id}/history` : Historique

#### 2.2 MaterialsController
**Fichier** : `backend/LaborControl.API/Controllers/MaterialsController.cs`

Endpoints :
- CRUD standard
- `POST /api/materials/{id}/ccpu-validate` : Validation CCPU
- `POST /api/materials/{id}/upload-certificate` : Upload certificat

#### 2.3 NDTControlsController
**Fichier** : `backend/LaborControl.API/Controllers/NDTControlsController.cs`

Endpoints :
- CRUD standard
- `GET /api/ndt-controls/weld/{weldId}` : Par soudure
- `POST /api/ndt-controls/{id}/submit-result` : Saisie résultat (contrôleur)
- `POST /api/ndt-controls/{id}/upload-report` : Upload rapport

#### 2.4 NDTProgramsController
**Fichier** : `backend/LaborControl.API/Controllers/NDTProgramsController.cs`

Endpoints :
- CRUD standard
- `POST /api/ndt-programs/generate` : Génération par IA (appelle agent)
- `POST /api/ndt-programs/{id}/approve` : Approbation
- `GET /api/ndt-programs/equipment/{equipmentId}` : Par équipement

#### 2.5 NonConformitiesController
**Fichier** : `backend/LaborControl.API/Controllers/NonConformitiesController.cs`

Endpoints :
- CRUD standard
- `POST /api/non-conformities/{id}/add-corrective-action` : Action corrective
- `POST /api/non-conformities/{id}/close` : Clôture FNC

#### 2.6 WelderQualificationsController
**Fichier** : `backend/LaborControl.API/Controllers/WelderQualificationsController.cs`

Endpoints :
- CRUD standard
- `GET /api/welder-qualifications/user/{userId}` : Par utilisateur
- `POST /api/welder-qualifications/{id}/approve` : Validation coordinateur
- `POST /api/welder-qualifications/validate-ai` : Pré-validation par IA

#### 2.7 TechnicalDocumentsController
**Fichier** : `backend/LaborControl.API/Controllers/TechnicalDocumentsController.cs`

Endpoints :
- CRUD standard
- `POST /api/technical-documents/upload` : Upload document
- `GET /api/technical-documents/equipment/{equipmentId}` : Par équipement
- `GET /api/technical-documents/download/{id}` : Téléchargement

#### 2.8 DMOSController
**Fichier** : `backend/LaborControl.API/Controllers/DMOSController.cs`

Endpoints :
- CRUD standard
- `POST /api/dmos/{id}/approve` : Approbation
- `POST /api/dmos/upload` : Upload DMOS

### Checklist Sprint 2
- [ ] 8 contrôleurs créés
- [ ] Tous les endpoints CRUD testés
- [ ] Authentification JWT configurée
- [ ] Autorisation par rôle implémentée
- [ ] Tests Postman/HTTP files créés
- [ ] Validation des données (Data Annotations)

---

## Sprint 3 : Agents IA & Services (4 jours - 2-5 jan)

### Objectif
Implémenter les 3 agents IA prioritaires pour le MVP avec Claude (Anthropic) et Gemini (Google).

**Architecture Multi-IA** : Voir `docs/AI_ARCHITECTURE.md` pour tous les détails.

### Répartition IA
- **Claude 3.5 Sonnet** : Génération procédures et programmes CND (raisonnement complexe)
- **Gemini 2.0 Flash** : Extraction données depuis PDF/images (vision + OCR)

### Tasks

#### 3.1 Service Azure Blob Storage
**Fichier** : `backend/LaborControl.API/Services/AzureBlobService.cs`

Fonctions :
- Upload file → retourne URL
- Download file
- Delete file
- Liste files par container

Containers :
- `technical-documents`
- `ndt-programs`
- `ndt-reports`
- `certificates`
- `dmos`

#### 3.2 Services IA
**Fichiers** :
- `backend/LaborControl.API/Services/AI/ClaudeService.cs`
- `backend/LaborControl.API/Services/AI/GeminiService.cs`
- `backend/LaborControl.API/Services/AI/AIOrchestrator.cs`

**Packages requis** :
```bash
dotnet add package Anthropic.SDK
dotnet add package Google.Cloud.AIPlatform.V1
```

**ClaudeService** :
- GenerateTextAsync() : Génération texte
- GenerateStructuredAsync<T>() : Génération JSON structuré

**GeminiService** :
- ExtractDataFromImageAsync<T>() : Extraction données depuis image/PDF
- AnalyzeImageAsync() : Analyse vision

**AIOrchestrator** :
- RouteRequest<T>() : Routage intelligent vers Claude ou Gemini

#### 3.3 Agent 1 : PreValidationQualificationAgent
**Fichier** : `backend/LaborControl.API/Services/AI/PreValidationQualificationAgent.cs`

**IA utilisée** : 🟢 **Gemini 2.0 Flash** (vision + OCR)

**Input** :
- PDF/Image de qualification soudeur/contrôleur

**Process** :
1. Upload vers Azure Blob
2. Appel Gemini pour extraction OCR
3. Parse JSON structuré

**Output** :
```json
{
  "qualificationNumber": "CERT-12345",
  "holderName": "Jean Dupont",
  "weldingProcess": "TIG",
  "materials": "Acier inox 304L",
  "thicknessRange": "3-10mm",
  "diameterRange": "DN50-DN200",
  "issueDate": "2024-01-15",
  "expirationDate": "2027-01-15",
  "issuingBody": "Bureau Veritas",
  "confidence": 0.95,
  "warnings": []
}
```

**Endpoint** : `POST /api/ai/validate-qualification`

#### 3.4 Agent 2 : NDTProgramGeneratorAgent
**Fichier** : `backend/LaborControl.API/Services/AI/NDTProgramGeneratorAgent.cs`

**IA utilisée** : 🔵 **Claude 3.5 Sonnet** (génération structurée complexe)

**Input** :
```json
{
  "equipmentId": 123,
  "welds": [
    {
      "reference": "S-001",
      "diameter": "DN100",
      "thickness": "6mm",
      "weldClass": "B",
      "material1": "316L",
      "material2": "316L"
    }
  ],
  "applicableStandards": ["RCC-M", "EN ISO 17640"],
  "cdcReference": "CDC-ORANO-2024-001"
}
```

**Process** :
1. Récupère CDC depuis Azure Blob
2. Récupère normes EDF depuis serveur
3. Appel Claude avec prompt structuré :
   - "Tu es un expert CND nucléaire"
   - Context : welds + CDC + normes
   - Task : Générer programme CND conforme
4. Parse réponse structurée (JSON mode)
5. Génère PDF avec QuestPDF
6. Upload vers Azure Blob

**Output** :
```json
{
  "programReference": "PROG-CND-001",
  "filePath": "https://blob.../ndt-programs/PROG-CND-001.pdf",
  "controls": [
    {
      "weldReference": "S-001",
      "controlType": "VT",
      "standard": "EN ISO 17637",
      "acceptanceCriteria": "Niveau B",
      "timing": "Avant PT"
    },
    {
      "weldReference": "S-001",
      "controlType": "PT",
      "standard": "EN ISO 3452",
      "acceptanceCriteria": "Niveau 2",
      "timing": "Après soudage"
    }
  ]
}
```

**Endpoint** : `POST /api/ai/generate-ndt-program`

#### 3.5 Agent 3 : ProcedureGeneratorAgent
**Fichier** : `backend/LaborControl.API/Services/AI/ProcedureGeneratorAgent.cs`

**IA utilisée** : 🔵 **Claude 3.5 Sonnet** (génération procédures détaillées)

**Input** :
```json
{
  "operationType": "Soudage TIG acier inox",
  "cdcReference": "CDC-ORANO-2024-001",
  "applicableStandards": ["RCC-M", "EN 1090"],
  "specificRequirements": "Zone contrôlée, procédure qualifiée"
}
```

**Process** :
1. Récupère CDC + normes
2. Appel Claude avec prompt spécifique
3. Génère procédure structurée (JSON)
4. Génère PDF avec QuestPDF
5. Upload vers Azure Blob

**Output** :
```json
{
  "procedureReference": "PROC-WELD-TIG-001",
  "filePath": "https://blob.../procedures/PROC-WELD-TIG-001.pdf",
  "sections": [
    {
      "title": "Préparation",
      "steps": ["Vérifier qualification soudeur", "..."]
    },
    {
      "title": "Exécution",
      "steps": ["Préparer chanfrein", "..."]
    },
    {
      "title": "Contrôles",
      "steps": ["VT pendant soudage", "..."]
    }
  ]
}
```

**Endpoint** : `POST /api/ai/generate-procedure`

### Checklist Sprint 3
- [ ] AzureBlobService opérationnel
- [ ] ClaudeService opérationnel (Anthropic SDK)
- [ ] GeminiService opérationnel (Google AI)
- [ ] AIOrchestrator implémenté
- [ ] Agent 1 : PreValidationQualificationAgent testé (Gemini)
- [ ] Agent 2 : NDTProgramGeneratorAgent testé (Claude)
- [ ] Agent 3 : ProcedureGeneratorAgent testé (Claude)
- [ ] PDF générés conformes
- [ ] Gestion erreurs IA (retry, fallback)
- [ ] Configuration API keys (Claude + Gemini)

---

## Sprint 4 : Mobile App (4 jours - 6-9 jan)

### Objectif
Adapter l'app mobile pour les nouveaux profils et workflows nucléaires.

### Tasks

#### 4.1 Nouveaux Écrans Soudeur
**Fichiers** : `mobile/app/(welder)/`

Écrans :
1. `tasks.tsx` : Liste tâches de soudage assignées
2. `weld-execution.tsx` : Saisie exécution soudure
   - Scan NFC équipement → Liste soudures
   - Sélection soudure
   - Confirmation exécution
   - Upload photo (optionnel)
3. `my-qualifications.tsx` : Ses qualifications

#### 4.2 Nouveaux Écrans Contrôleur CND
**Fichiers** : `mobile/app/(ndt-controller)/`

Écrans :
1. `controls.tsx` : Liste contrôles à réaliser
2. `ndt-control.tsx` : Saisie résultat contrôle
   - Scan NFC équipement → Liste soudures
   - Sélection soudure + type contrôle
   - Saisie résultat (Conforme/Non-conforme)
   - Upload photos
   - Commentaires
3. `my-qualifications.tsx` : Ses qualifications CND

#### 4.3 Nouveaux Écrans CCPU
**Fichiers** : `mobile/app/(ccpu)/`

Écrans :
1. `pending-materials.tsx` : Matériaux en attente validation
2. `material-validation.tsx` : Validation matériau
   - Affichage certificat
   - Approbation/Rejet
   - Commentaires
3. `pending-welds.tsx` : Soudures en attente validation
4. `weld-validation.tsx` : Validation soudure

#### 4.4 Service API Mobile
**Fichier** : `mobile/services/api.ts`

Ajouter endpoints :
```typescript
// Welds
getWeldsByEquipment(equipmentId: number)
executeWeld(weldId: number, data: WeldExecutionData)
getMyWelds()

// NDT Controls
getNDTControlsByWeld(weldId: number)
submitNDTControl(data: NDTControlData)
uploadNDTReport(controlId: number, file: File)

// Materials
getPendingMaterials()
validateMaterial(materialId: number, approved: boolean, comments: string)

// Qualifications
getMyQualifications()
uploadQualification(file: File)
```

#### 4.5 Offline Store
**Fichier** : `mobile/store/weldStore.ts`

State management pour :
- Liste soudures hors ligne
- Queue de synchronisation exécutions
- Queue de synchronisation contrôles

#### 4.6 NFC Integration
**Fichier** : `mobile/services/nfc.ts`

Adapter pour :
- Lecture tag NFC équipement
- Récupération liste soudures associées
- Affichage modal sélection soudure

### Checklist Sprint 4
- [ ] 9 nouveaux écrans créés
- [ ] Service API étendu
- [ ] NFC adapté pour équipements/soudures
- [ ] Offline store fonctionnel
- [ ] Tests sur device Android/iOS
- [ ] Upload photos opérationnel

---

## Sprint 5 : Workflows & Finitions (3 jours - 10-12 jan)

### Objectif
Implémenter les workflows de verrouillage et finaliser le MVP.

### Tasks

#### 5.1 Service Workflow
**Fichier** : `backend/LaborControl.API/Services/WorkflowService.cs`

Logique de verrouillage :

```csharp
public class WorkflowService
{
    // Vérifie si matériau peut être débité
    public async Task<bool> CanCutMaterial(int materialId)
    {
        var material = await _context.Materials.FindAsync(materialId);
        return material.Status == "Approved";
    }

    // Vérifie si soudeur peut souder
    public async Task<bool> CanWelderExecuteWeld(int welderId, Weld weld)
    {
        var qualifications = await _context.WelderQualifications
            .Where(q => q.UserId == welderId && q.Status == "Valid")
            .ToListAsync();

        // Vérifier process, matériaux, diamètre, épaisseur
        return qualifications.Any(q =>
            q.WeldingProcess == weld.WeldingProcess &&
            // ... autres vérifications
        );
    }

    // Vérifie si CND peut être effectué
    public async Task<bool> CanPerformNDT(int weldId)
    {
        var weld = await _context.Welds.FindAsync(weldId);
        return weld.Status == "Welded";
    }

    // Bloque une soudure
    public async Task BlockWeld(int weldId, string reason)
    {
        var weld = await _context.Welds.FindAsync(weldId);
        weld.IsBlocked = true;
        weld.BlockReason = reason;
        await _context.SaveChangesAsync();
    }
}
```

#### 5.2 Middleware Workflow
**Fichier** : `backend/LaborControl.API/Middleware/WorkflowValidationMiddleware.cs`

Intercepte les requêtes et valide les workflows :
- POST /api/welds/{id}/execute → Vérifie qualifications soudeur
- POST /api/ndt-controls → Vérifie soudure exécutée
- POST /api/materials/cut → Vérifie validation CCPU

#### 5.3 Dashboard Basique
**Fichier** : `mobile/app/(admin)/dashboard.tsx`

KPI basiques :
- Nombre soudures (prévues/exécutées/validées)
- Taux de conformité CND
- Nombre FNC ouvertes
- Liste dernières activités

#### 5.4 Notifications
**Fichier** : `backend/LaborControl.API/Services/NotificationService.cs`

Notifications par email (Azure Communication Services) :
- CCPU → Nouveau matériau à valider
- Coordinateur → Nouvelle qualification à valider
- Contrôleur → Nouvelle soudure à contrôler
- Qualité → Nouvelle FNC créée

#### 5.5 Documentation API
**Fichier** : `backend/LaborControl.API/Program.cs`

Activer Swagger avec exemples :
```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LABOR CONTROL DMTT API",
        Version = "v1",
        Description = "API pour démantèlement nucléaire Tricastin"
    });

    // Ajouter exemples
    options.EnableAnnotations();
});
```

#### 5.6 Tests End-to-End
Scénarios à tester :

**Scénario 1 : Réception matériau → Soudage**
1. Sous-traitant upload certificat matériau
2. CCPU valide matériau
3. Soudeur scanne NFC équipement
4. Soudeur sélectionne soudure
5. Système vérifie qualifications
6. Soudeur exécute soudure

**Scénario 2 : Soudure → Contrôle CND**
1. Soudure marquée "Welded"
2. Contrôleur scanne NFC
3. Contrôleur saisit résultat VT
4. Contrôleur upload photo
5. CCPU valide soudure

**Scénario 3 : Non-conformité**
1. Contrôleur détecte défaut
2. Création FNC automatique
3. Notification qualité
4. Ajout action corrective
5. Re-contrôle

#### 5.7 Déploiement Azure
**Étapes** :
1. Créer App Service Plan
2. Créer Web App pour backend
3. Créer PostgreSQL flexible server
4. Configurer Blob Storage
5. Configurer API keys (Claude + Gemini dans Key Vault)
6. Configurer Key Vault
7. Deploy backend
8. Appliquer migrations
9. Seed données de test

### Checklist Sprint 5
- [ ] WorkflowService implémenté
- [ ] Middleware validation fonctionnel
- [ ] Dashboard basique créé
- [ ] Notifications email opérationnelles
- [ ] Swagger documenté
- [ ] 3 scénarios end-to-end testés
- [ ] Déploiement Azure effectué
- [ ] MVP validé fonctionnel

---

## Configuration Environnement

### Backend Local
```bash
cd labor-control-dmtt/backend/LaborControl.API

# Restore packages
dotnet restore

# Create appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=laborcontrol_dmtt;Username=postgres;Password=yourpassword"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-min-32-chars",
    "Issuer": "LABORCONTROL-DMTT",
    "Audience": "LABORCONTROL-DMTT-API"
  },
  "Claude": {
    "ApiKey": "sk-ant-api03-...",
    "Model": "claude-3-5-sonnet-20241022",
    "MaxTokens": 4096,
    "Temperature": 0.3
  },
  "Gemini": {
    "ApiKey": "AIza...",
    "ProjectId": "your-gcp-project-id",
    "Location": "us-central1",
    "Model": "gemini-2.0-flash-exp"
  },
  "AzureBlobStorage": {
    "ConnectionString": "your-connection-string",
    "ContainerName": "documents"
  }
}

# Run
dotnet run
```

### Mobile Local
```bash
cd labor-control-dmtt/mobile

# Install dependencies
npm install

# Create .env
API_URL=http://localhost:5000/api
ENVIRONMENT=development

# Start
npx expo start
```

---

## Risques & Mitigation

### Risque 1 : Délais serrés (19 jours)
**Mitigation** :
- Scope MVP strict (pas de features hors scope)
- Sprints courts avec démos journalières
- Parallélisation backend/mobile

### Risque 2 : Complexité Agents IA
**Mitigation** :
- Commencer avec prompts simples
- Itérer sur qualité output
- Fallback manuel si IA échoue

### Risque 3 : Intégration Azure
**Mitigation** :
- Setup Azure dès Sprint 1
- Tests continus sur cloud
- Environnement de staging

### Risque 4 : NFC sur Terrain
**Mitigation** :
- Fallback saisie manuelle
- Tests sur vrais devices
- Mode dégradé hors ligne

---

## Livrables Finaux MVP

### Backend
- ✅ API .NET Core 9.0 déployée sur Azure
- ✅ 8 nouveaux contrôleurs
- ✅ 9 nouvelles entités
- ✅ 3 agents IA fonctionnels
- ✅ Workflows de verrouillage
- ✅ Documentation Swagger

### Mobile
- ✅ App React Native pour 3 profils (Soudeur, Contrôleur, CCPU)
- ✅ NFC opérationnel
- ✅ Mode offline-first
- ✅ Upload photos

### Infrastructure
- ✅ Azure App Service
- ✅ Azure PostgreSQL
- ✅ Azure Blob Storage
- ✅ Claude API (Anthropic) + Gemini API (Google)
- ✅ Azure Key Vault

### Documentation
- ✅ Architecture analysis
- ✅ API documentation (Swagger)
- ✅ Guide utilisateur basique
- ✅ Guide déploiement

---

## Post-MVP (Phase 2 - après 12 janvier)

### Features à Ajouter
1. **Planning Gantt automatique** (Agent 5)
2. **Tableaux de bord avancés** (Power BI integration)
3. **Export dossiers fabrication complets**
4. **Gestion FNC complète** (workflow approbation)
5. **Module inspecteur EDF** (validation finale)
6. **Dashboard Blazor web** (gestion centralisée)
7. **Reporting avancé** (conformité, KPI, tendances)

---

## Contact & Support

**Chef de Projet** : À définir
**Développeur Backend** : Claude Code (Multi-Agent)
**Développeur Mobile** : Claude Code (Multi-Agent)
**Deadline MVP** : 12 janvier 2025
