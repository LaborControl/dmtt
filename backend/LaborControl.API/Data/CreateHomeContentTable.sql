-- Script pour créer la table HomeContents et insérer les données seed
-- À exécuter sur Azure PostgreSQL

-- Créer la table HomeContents
CREATE TABLE IF NOT EXISTS "HomeContents" (
    "Id" uuid NOT NULL,
    "Content" text NOT NULL,
    "IsPublished" boolean NOT NULL,
    "PublishedAt" timestamp with time zone NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "Version" integer NOT NULL,
    CONSTRAINT "PK_HomeContents" PRIMARY KEY ("Id")
);

-- Créer un index sur IsPublished pour optimiser les requêtes
CREATE INDEX IF NOT EXISTS "IX_HomeContents_IsPublished" ON "HomeContents" ("IsPublished");

-- Insérer les données seed
INSERT INTO "HomeContents" ("Id", "Content", "IsPublished", "PublishedAt", "CreatedAt", "UpdatedAt", "Version")
VALUES (
    '550e8400-e29b-41d4-a716-446655440000'::uuid,
    '{
      "hero": {
        "badge": "25-30% d''économies annuelles prouvées",
        "title": "Réduisez vos coûts maintenance de 25-30%",
        "subtitle": "La seule solution de traçabilité terrain qui élimine les omissions via scan NFC obligatoire. Conformité ISO garantie, zéro litige sur la preuve de présence.",
        "cta1": {
          "text": "Calculer vos économies",
          "link": "#contact"
        },
        "cta2": {
          "text": "Voir un cas client",
          "link": "#solution"
        },
        "stats": "✓ 2,3M€ économisés par nos clients en 2024 | ✓ 3000+ points équipés | ✓ ISO 27001"
      },
      "painPoints": [
        {
          "title": "Arrêts non planifiés",
          "description": "Coûtent 150k€/h en raffinage. Causés par maintenance défaillante non tracée.",
          "icon": "⚠️",
          "stat": "150k€/h perdu par arrêt imprévu"
        },
        {
          "title": "Non-conformité ISO",
          "description": "Absence de preuve d''audit : perte de certification, exclusion appels d''offres.",
          "icon": "📋",
          "stat": "Millions en marché perdu"
        },
        {
          "title": "Risques juridiques",
          "description": "En cas d''accident : impossible de prouver que les contrôles ont été faits.",
          "icon": "⚖️",
          "stat": "Responsabilité personnelle"
        }
      ],
      "solution": {
        "title": "Comment LABOR CONTROL résout le problème",
        "description": "Résultats mesurables",
        "features": [
          {
            "title": "-30% Temps administratif",
            "description": "Automatisation saisies via NFC, plus de recopie de données fictives"
          },
          {
            "title": "-25% Coûts maintenance",
            "description": "Maintenance préventive basée sur données réelles vs urgence"
          },
          {
            "title": "100% Traçabilité audits",
            "description": "Preuve numérique incontestable pour ISO 9001/55001/HAS"
          },
          {
            "title": "0 Arrêts liés à non-conformité",
            "description": "Élimination des arrêts dus à contrôles manqués"
          }
        ],
        "caseStudy": {
          "title": "Cas réel : Raffinerie 500 employés",
          "before": {
            "label": "Avant LABOR CONTROL",
            "value": "2.5M€/an coûts maintenance"
          },
          "after": {
            "label": "Après 6 mois",
            "value": "1.8M€/an (-28%)"
          },
          "roi": {
            "label": "ROI (puce NFC 80€ × 150 points)",
            "value": "Payé en 9 jours"
          },
          "quote": "On savait qu''on perdait de l''argent, mais sans LABOR CONTROL on n''avait aucune visibilité sur où. Maintenant c''est clair : chaque point d''intervention tracé = -15% coûts en moyenne.",
          "author": "Jean-Luc M., Directeur Maintenance"
        }
      },
      "testimonials": [
        {
          "rating": 5,
          "quote": "Avant LABOR CONTROL, on n''avait aucune visibilité sur les contrôles réalisés. Maintenant, chaque intervention est tracée, et on a réduit nos coûts de 28%.",
          "author": "Jean-Luc Moreau",
          "role": "Directeur Maintenance, Raffinerie Total"
        },
        {
          "rating": 5,
          "quote": "La conformité FDA/EMA est maintenant garantie. Plus besoin de panique lors des audits : toute la traçabilité est numérique et incontestable.",
          "author": "Marie Dubois",
          "role": "Quality Manager, Pharma Sanofi"
        },
        {
          "rating": 5,
          "quote": "HACCP traçable = tranquillité pour nous. Une crise alimentaire coûte 10M€. LABOR CONTROL nous l''a évitée. Meilleur investissement ever.",
          "author": "Sophie Lefevre",
          "role": "Operations Manager, Danone"
        }
      ],
      "pricing": {
        "note": "Au-delà de 10 points, tarification dégressives et sur devis. Exemple : 50 points = 150€ HT/an"
      },
      "faq": [
        {
          "question": "Intégration GMAO existante ?",
          "answer": "Oui. Nos APIs s''intègrent avec Maximo, SAP PM, Infor EAM, Copier. Connecteur natif ou webhook."
        },
        {
          "question": "Temps de déploiement ?",
          "answer": "Typiquement 2 semaines : audit site, encodage puces, formation techniciens, tests. Clé en main."
        },
        {
          "question": "Support 24/7 ?",
          "answer": "Oui, inclus. Support hotline + chat + email. SLA 2h pour problèmes critiques."
        },
        {
          "question": "Et après ? Formation continu ?",
          "answer": "Oui. Onboarding complet + documentation + webinars mensuels + coaching."
        }
      ]
    }',
    true,
    NOW(),
    NOW(),
    NOW(),
    1
)
ON CONFLICT ("Id") DO NOTHING;

-- Ajouter à la table de migrations EF Core pour éviter les conflits
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251029011313_AddHomeContentManagement', '9.0.0')
ON CONFLICT DO NOTHING;
