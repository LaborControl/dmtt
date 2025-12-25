# LABOR CONTROL DMTT - Instructions de Setup

## Contexte
Création d'un fork complet de LABOR CONTROL pour le marché du démantèlement nucléaire.
Nom du projet : **LABOR CONTROL DMTT** (Démantèlement)

## Mission
Système de traçabilité et contrôle qualité pour les opérations de démantèlement nucléaire du site du Tricastin (usine Eurodif).

## Objectif MVP - 12 janvier 2025
1. Traçabilité des tâches de contrôle (soudure, CND, CCPU, etc.)
2. Génération automatique des procédures par IA à partir du CDC ORANO
3. Multi-profils utilisateurs spécifiques nucléaire

## Repositories à cloner

### Repos essentiels pour LABOR CONTROL DMTT :
- `LaborControl/Backend` (.NET Core)
- `LaborControl/mobile` (React Native + Watermelon DB)
- `LaborControl/web-dashboard` (Blazor)
- `LaborControl/laborcontrol-shared` (librairies communes)

### Token GitHub
```
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
```

## Structure cible
```
labor-control-dmtt/
├── backend/
├── mobile/
├── web-dashboard/
├── shared/
└── docs/
```

## Étapes de clonage

1. Créer le dossier principal `labor-control-dmtt`
2. Cloner les 4 repos essentiels avec le token
3. Renommer les repos clonés selon la structure cible
4. Vérifier l'architecture de chaque repo

## Commandes Git avec token
```bash
git clone https://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@github.com/LaborControl/Backend.git
git clone https://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@github.com/LaborControl/mobile.git
git clone https://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@github.com/LaborControl/web-dashboard.git
git clone https://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@github.com/LaborControl/laborcontrol-shared.git
```

## Stack technique confirmée
- Backend : .NET Core
- Frontend Web : Blazor
- Mobile : React Native avec Watermelon DB (offline-first)
- Infrastructure : Azure
- Base de données : À confirmer (probablement SQL Server ou PostgreSQL)
- NFC : Validation et traçabilité

## Spécificités métier nucléaire

### Entités principales
- **Soudures** (repère, diamètre, épaisseur, matériaux, procédé, classe, contrôles requis, DMOS)
- **Équipements** (systèmes de démantèlement : ponts roulants, robots)
- **Matériaux** (avec CCPU - validation conformité CDC/normes)
- **Qualifications** (soudeurs, contrôleurs CND)
- **Contrôles** (VT, PT, MT, RT, UT)
- **FNC** (Fiches de Non-Conformité)
- **Documents** (plans BE, CDC, certificats, DMOS, normes EDF)

### Profils utilisateurs
1. Sous-traitant (dépôt documents)
2. Soudeur (saisie exécution)
3. Contrôleur CND (saisie contrôles)
4. CCPU (validation matériaux/soudures)
5. Coordinateur soudage (validation qualifications)
6. Responsable qualité (validation FNC, programmes CND, procédures)
7. Inspecteur EDF (validation finale)
8. Planificateur (gestion Gantt, ressources, priorités)

### Workflows de verrouillage
1. Réception matériaux → CCPU valide → Débit autorisé
2. Qualifications soudeurs → Coordinateur valide → Soudage autorisé
3. Soudure exécutée → Contrôle CND autorisé
4. CND validé → CCPU valide → Étape suivante

### Agents IA requis
1. **Agent Pré-validation Qualifications** : Analyse documents soudeurs/contrôleurs
2. **Agent Génération Programme CND** : À partir caractéristiques soudures + normes EDF + CDC
3. **Agent Génération Procédures** : À partir CDC ORANO + normes
4. **Agent Adaptation CND** : Adaptation suite FNC
5. **Agent Planification** : Génération Gantt automatique avec dépendances

### Traçabilité
- NFC par équipement ou partie d'équipement (pas par soudure individuelle)
- Scan NFC → Liste soudures associées → Sélection
- Saisie manuelle en complément

### Documents
**Entrée :**
- Plans BE (PDF/DWG)
- Cahier des charges
- Certificats matériaux
- Qualifications soudeurs/contrôleurs
- Procédures de soudage (DMOS)
- Normes EDF

**Sortie :**
- Programmes de CND (générés par IA)
- Rapports de contrôle CND
- Dossiers de fabrication par équipement
- Certificats de conformité
- Historique traçabilité par soudure
- Procédures spécifiques projet

### Tableaux de bord
Différenciés par profil avec KPI :
- Avancement soudures (prévues/réalisées/validées)
- Taux de conformité
- FNC en cours
- Retards planning

## Spécificités démantèlement Tricastin
- Site : Usine Eurodif (Georges Besse I) - diffusion gazeuse
- Période : 1979-2012, démantèlement jusqu'en 2051
- Volumes : 1 300 km tuyauteries, 160 000 tonnes acier, 1 400 étages diffusion
- Lot Europe Technologie : Tuyauteries acier carbone (circuits auxiliaires)
- Contrôles avant découpe : Contamination surfacique uranium + débit de dose → décision sas

## Dashboard LABOR CONTROL Original (Maintenance)

### Modules existants à analyser et adapter :
1. **Compte** - Conservé
2. **Métiers et Qualifications** - À adapter pour nucléaire (soudeurs, contrôleurs CND, CCPU, etc.)
3. **Services / Équipes** - Devient **Entreprises / Équipes** (sous-traitants)
4. **Gestion du personnel** - Devient **Gestion des intervenants**
5. **Sites** - Devient **Projet** (contexte projet unique)
6. **Zones** - Conservé (zones Tricastin)
7. **Équipements** - **ESSENTIEL** - Adapter pour éléments/sous-éléments avec hiérarchie
8. **Gammes de maintenance** - Devient **Programmes de contrôle** (CND, validations)
9. **Pièces de rechange** - **À évaluer** (pertinent pour DMTT ?)
10. **Paramètres** - Conservé + ajout paramètres nucléaires
11. **Points de Contrôle** - Devient **Éléments à contrôler** (soudures, équipements)
12. **Planification** - **ESSENTIEL** - Gantt projet avec dépendances
13. **Historique interventions** - Devient **Historique fabrication/contrôles**
14. **Protocoles** - Devient **Procédures / MODOP**
15. **Tableau de bord** - **Dashboard projet** avec KPIs

### Nouveaux modules DMTT à créer :
16. **Normes & IA** - Module IA RQ (normes, procédures, chat, veille)
17. **Soudures** - Gestion spécifique soudures avec caractéristiques
18. **Matériaux** - Validation CCPU conformité CDC/normes
19. **FNC** - Fiches Non-Conformité avec workflow
20. **Documents projet** - CDC, plans BE, rapports
21. **NFC Éléments** - Attribution puces NFC + historique selon rôle

## Adaptation Dashboard RQ - Vision PROJET

### Structure dashboard RQ DMTT :
**Section 1 : Vue Projet Global**
- Nom projet
- Avancement global (%)
- Jalons principaux
- Budget (si applicable)
- KPIs synthétiques

**Section 2 : Normes & IA** ⭐ NOUVEAU
- Upload normes
- Consultation base normes
- Chat IA technique
- Génération procédures/MODOP
- Analyse CDC/plans
- Alertes veille normes
- Agent surveillance temps réel

**Section 3 : Documents Projet**
- CDC
- Plans BE
- Procédures générées
- Rapports
- Certificats

**Section 4 : Éléments & Sous-éléments**
- Arborescence équipements (hiérarchique)
- Statuts fabrication
- Attribution puces NFC
- Soudures par élément

**Section 5 : Planning Projet** (Gantt)
- Gantt interactif
- Dépendances
- Ressources
- Alertes retards

**Section 6 : Contrôles & Validations**
- Programme CND
- Validations en attente (matériaux, qualifications, soudures, CND)
- Historique validations

**Section 7 : FNC**
- FNC ouvertes
- FNC en traitement
- FNC clôturées
- Actions recommandées IA

**Section 8 : Intervenants**
- Entreprises sous-traitantes
- Intervenants par rôle
- Qualifications

**Section 9 : Configuration**
- Rôles personnalisés (création/modification)
- Niveaux infos NFC par rôle (4 niveaux)
- Workflows FNC configurables
- Paramètres projet

## NFC - Gestion Éléments/Sous-éléments

### Principe
- Puce NFC = Identifiant physique d'un élément ou sous-élément
- **Éléments** : Équipements complets (pont roulant, robot complet)
- **Sous-éléments** : Parties d'équipements (chemin roulement, chariot, support robot, outil robot)
- Hiérarchie : Sous-éléments "montés avec" pour former Éléments

### Workflow création éléments (avec IA)
1. RQ upload plans BE + CDC
2. Agent IA analyse documents :
   - Extrait liste éléments (équipements complets)
   - Extrait liste sous-éléments (parties)
   - Détecte relations hiérarchiques "monté avec"
   - Extrait métadonnées (nom, réf, destination, position)
   - Extrait soudures associées
3. IA crée automatiquement en base :
   - Table éléments avec hiérarchie
   - Soudures liées
   - Métadonnées complètes
4. RQ valide/corrige si nécessaire

### Attribution puce NFC (terrain)
1. RQ ou adjoint sur terrain avec app mobile
2. Scan nouvelle puce NFC vierge
3. App affiche liste éléments/sous-éléments créés par IA (non encore liés)
4. Sélection élément concerné
5. Attribution → Lien physique/numérique enregistré
6. Puce maintenant active pour cet élément

### Scan NFC - Niveaux infos selon rôle (paramétrable par RQ)

**Niveau 1 - Info Simple :**
- Nom élément
- Référence
- Destination finale
- Position

**Niveau 2 - Hiérarchie :**
+ Monté avec (liste sous-éléments ou élément parent)
+ Arborescence complète

**Niveau 3 - Fabrication :**
+ Liste soudures associées
+ Statut soudures (planifiées/en cours/terminées/validées)
+ Statut global élément

**Niveau 4 - Historique Complet :**
+ Tous contrôles effectués (dates, opérateurs, résultats)
+ FNC liées (avec traitements)
+ Documents liés (rapports CND, certificats)
+ Timeline complète fabrication

**Configuration par RQ :**
- Pour chaque rôle créé, RQ assigne niveau(s) autorisé(s)
- Ex : Soudeur → Niveau 1 uniquement
- Ex : Auditeur → Niveau 4 complet
- Ex : Client → Niveaux 1, 2, 3 (pas historique détaillé)

## Prochaines étapes après clonage

### Phase 1 : Analyse existant
1. **Analyser dashboard actuel** (modules, navigation, structure)
2. **Analyser architecture backend** (.NET Core, controllers, services, repositories)
3. **Analyser schéma base données** (tables, relations)
4. **Analyser frontend** (Blazor components, pages, layouts)
5. **Analyser app mobile** (React Native, Watermelon DB, sync)

### Phase 2 : Adaptation intelligente DMTT
6. **Identifier réutilisable tel quel** (auth, compte, paramètres)
7. **Identifier à adapter** (équipements → éléments hiérarchiques, personnel → intervenants)
8. **Identifier à supprimer** (pièces de rechange ?, gammes maintenance récurrentes)
9. **Identifier nouveaux modules** (normes & IA, soudures, matériaux, FNC, NFC éléments)

### Phase 3 : Développement DMTT
10. Définir schéma base données DMTT complet (avec migrations)
11. Créer nouvelles entités métier nucléaires
12. Adapter modules existants (renommage, logique métier)
13. Créer nouveaux modules (normes & IA, soudures, FNC, etc.)
14. Développer agents IA (10 agents spécialisés)
15. Implémenter workflows de verrouillage
16. Développer module NFC éléments avec niveaux infos
17. Adapter dashboard RQ vision projet
18. Créer dashboards différenciés par rôle

## Notes importantes
- Deadline : 12 janvier 2025
- Claude Code travaille en multi-agents
- Infrastructure : déploiement Azure uniquement (pas de local)
- Tous les documents normes EDF seront disponibles sur serveur pour lecture par agent IA
