-- Seed data pour HomeContent - Contenu initial de la page d'accueil
-- Exécuter après migration EF Core

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
      "useCases": [
        {
          "id": "raffinerie",
          "title": "Raffinerie & Chimie",
          "problem": "Arrêts non planifiés = 150k€/h. Impossible de prouver que les contrôles d''équipements critiques ont été réalisés. Maintenance réactive vs préventive = surcoûts majeurs.",
          "solution": [
            "Scan NFC obligatoire sur chaque équipement critique",
            "Alertes temps réel anomalies détectées",
            "Planification maintenance optimisée sur données réelles",
            "Conformité ISO 55001 (Asset Management) garantie"
          ],
          "roi": "850k€ économisés/an × 3 ans = 2,55M€"
        },
        {
          "id": "pharma",
          "title": "Pharma & Cosmétiques",
          "problem": "Contrôles chambre froide manqués = perte lots pharmaceutiques = 500k€/incident. FDA/EMA audits : absence preuve numérique = perte certification.",
          "solution": [
            "Scan NFC + photo géolocalisation pour chaque contrôle",
            "Historique complet tracé pour FDA/EMA",
            "Alertes anomalies avant rupture chaîne froid",
            "Conformité FDA 21 CFR Part 11 ready"
          ],
          "roi": "0 perte de lots + certification garantie = priceless"
        },
        {
          "id": "agroalim",
          "title": "Agroalimentaire",
          "problem": "Contrôles HACCP non tracés = risque alimentaire + amende DGCCRF. Retraçabilité en cas de crise = 10M€ de pertes (marque + perte marché).",
          "solution": [
            "Scan NFC + photo HACCP à chaque points critiques",
            "Traçabilité numérique DGCCRF compliant",
            "Alertes anomalies avant mise en rayon",
            "Mode offline : fonctionne en zones ATEX"
          ],
          "roi": "ROI : évite 1 crise = 10M€ économisés"
        }
      ],
      "compliance": [
        {
          "icon": "LOCK",
          "title": "ISO 27001",
          "description": "Sécurité info"
        },
        {
          "icon": "OK",
          "title": "RGPD",
          "description": "Conforme protection données"
        },
        {
          "icon": "FR",
          "title": "Données France",
          "description": "Hébergement souverain"
        },
        {
          "icon": "E2E",
          "title": "Encryption",
          "description": "Chiffrement bout-à-bout"
        }
      ],
      "antifraud": {
        "title": "Preuve anti-fraude : HMAC-SHA256",
        "description": "Chaque scan NFC génère une signature cryptographique incontestable. Impossible de cloner ou modifier les données après création. Acceptable comme preuve légale en audit ISO/FDA."
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
        "title": "Pricing Transparent",
        "plans": [
          {
            "name": "Puce NFC",
            "price": "80",
            "currency": "€",
            "period": "HT/puce",
            "type": "Investissement unique",
            "features": [
              "Sécurisée SHA256 anti-clonage",
              "Installation incluse",
              "Support technique 24/7",
              "Garantie 5 ans"
            ]
          },
          {
            "name": "Application",
            "price": "0",
            "currency": "€",
            "period": "/mois",
            "type": "GRATUIT jusqu''à 10 points",
            "features": [
              "Mobile iOS & Android",
              "Interface superviseur web",
              "Toutes les fonctionnalités",
              "Mode offline complet"
            ]
          }
        ],
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
      ],
      "footer": {
        "description": "Traçabilité maintenance industrielle via NFC. -25% coûts garantis.",
        "links": [
          {
            "title": "Solution",
            "items": [
              {"text": "Comment ça marche", "url": "#solution"},
              {"text": "Cas d''usage", "url": "#usecases"},
              {"text": "Conformité", "url": "#conformite"}
            ]
          },
          {
            "title": "Entreprise",
            "items": [
              {"text": "Pricing", "url": "#pricing"},
              {"text": "Connexion", "url": "/login"},
              {"text": "Contact", "url": "#contact"}
            ]
          }
        ],
        "contact": {
          "email": "contact@labor-control.fr",
          "website": "labor-control.fr",
          "location": "France (Souveraineté données)"
        }
      }
    }',
    true,
    NOW(),
    NOW(),
    NOW(),
    1
);
