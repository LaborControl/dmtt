namespace LaborControl.API.Data
{
    public static class IndustryMetadata
    {
        // Catégories disponibles par métier
        public static Dictionary<string, List<CategoryOption>> GetCategoriesByIndustry()
        {
            return new Dictionary<string, List<CategoryOption>>
            {
                // QHSE
                ["RESP_QHSE"] = new List<CategoryOption>
                {
                    new("INSPECTION", "Inspection QHSE"),
                    new("CONTROLE_REGLEMENTAIRE", "Contrôle réglementaire"),
                    new("AUDIT", "Audit"),
                    new("VERIFICATION", "Vérification")
                },
                ["ANIM_SEC"] = new List<CategoryOption>
                {
                    new("INSPECTION", "Inspection sécurité"),
                    new("CONTROLE_VISUEL", "Contrôle visuel"),
                    new("ANIMATION", "Animation sécurité")
                },
                ["AUDIT_Q"] = new List<CategoryOption>
                {
                    new("INSPECTION", "Audit qualité"),
                    new("VERIFICATION", "Vérification qualité"),
                    new("CONTROLE_REGLEMENTAIRE", "Contrôle réglementaire")
                },
                ["TECH_HSE"] = new List<CategoryOption>
                {
                    new("INSPECTION", "Inspection HSE"),
                    new("EVALUATION_RISQUES", "Évaluation des risques"),
                    new("VERIFICATION", "Vérification")
                },

                // Santé
                ["AS"] = new List<CategoryOption>
                {
                    new("CONTROLE_VISUEL", "Contrôle visuel"),
                    new("HYGIENE", "Contrôle hygiène"),
                    new("SOINS", "Soins")
                },
                ["IDE"] = new List<CategoryOption>
                {
                    new("CONTROLE_VISUEL", "Contrôle visuel"),
                    new("VERIFICATION", "Vérification"),
                    new("SOINS", "Soins infirmiers"),
                    new("HYGIENE", "Contrôle hygiène")
                },
                ["AVS"] = new List<CategoryOption>
                {
                    new("CONTROLE_VISUEL", "Contrôle visuel"),
                    new("ASSISTANCE", "Assistance"),
                    new("SECURITE", "Contrôle sécurité")
                },
                ["ASH"] = new List<CategoryOption>
                {
                    new("NETTOYAGE", "Nettoyage"),
                    new("DESINFECTION", "Désinfection"),
                    new("CONTROLE_VISUEL", "Contrôle visuel")
                },

                // Nettoyage
                ["AGENT_NET"] = new List<CategoryOption>
                {
                    new("NETTOYAGE", "Nettoyage"),
                    new("DESINFECTION", "Désinfection"),
                    new("CONTROLE_VISUEL", "Contrôle visuel")
                },
                ["APU"] = new List<CategoryOption>
                {
                    new("NETTOYAGE", "Nettoyage voirie"),
                    new("COLLECTE", "Collecte déchets"),
                    new("CONTROLE_VISUEL", "Contrôle visuel")
                },
                ["CHEF_NET"] = new List<CategoryOption>
                {
                    new("VERIFICATION", "Vérification qualité"),
                    new("CONTROLE_VISUEL", "Contrôle visuel"),
                    new("INSPECTION", "Inspection")
                },

                // Sécurité
                ["ADS"] = new List<CategoryOption>
                {
                    new("RONDE", "Ronde de sécurité"),
                    new("CONTROLE_VISUEL", "Contrôle visuel"),
                    new("VERIFICATION", "Vérification"),
                    new("SURVEILLANCE", "Surveillance")
                },
                ["CYNO"] = new List<CategoryOption>
                {
                    new("RONDE", "Ronde cynophile"),
                    new("CONTROLE_VISUEL", "Contrôle visuel"),
                    new("SURVEILLANCE", "Surveillance")
                },
                ["SSIAP"] = new List<CategoryOption>
                {
                    new("VERIFICATION", "Vérification incendie"),
                    new("CONTROLE_REGLEMENTAIRE", "Contrôle réglementaire"),
                    new("INSPECTION", "Inspection")
                },
                ["RONDIER"] = new List<CategoryOption>
                {
                    new("RONDE", "Ronde"),
                    new("CONTROLE_VISUEL", "Contrôle visuel"),
                    new("INTERVENTION", "Intervention")
                },

                // Commerce
                ["VENDEUR"] = new List<CategoryOption>
                {
                    new("CONTROLE_VISUEL", "Contrôle visuel"),
                    new("MERCHANDISING", "Merchandising"),
                    new("MESURE", "Inventaire")
                },
                ["CAISSIER"] = new List<CategoryOption>
                {
                    new("VERIFICATION", "Vérification caisse"),
                    new("CONTROLE", "Contrôle")
                },
                ["EMP_COM"] = new List<CategoryOption>
                {
                    new("CONTROLE_VISUEL", "Contrôle visuel"),
                    new("MISE_EN_RAYON", "Mise en rayon"),
                    new("VERIFICATION", "Vérification")
                },
                ["CHEF_RAYON"] = new List<CategoryOption>
                {
                    new("VERIFICATION", "Vérification"),
                    new("CONTROLE_VISUEL", "Contrôle visuel"),
                    new("GESTION_STOCK", "Gestion stock")
                },

                // Restauration
                ["CUISINIER"] = new List<CategoryOption>
                {
                    new("MESURE", "Contrôle températures"),
                    new("NETTOYAGE", "Nettoyage"),
                    new("HYGIENE", "Contrôle hygiène"),
                    new("PREPARATION", "Préparation")
                },
                ["SERVEUR"] = new List<CategoryOption>
                {
                    new("CONTROLE_VISUEL", "Contrôle visuel"),
                    new("MISE_EN_PLACE", "Mise en place"),
                    new("SERVICE", "Service")
                },
                ["COMMIS"] = new List<CategoryOption>
                {
                    new("CONTROLE_VISUEL", "Contrôle visuel"),
                    new("PREPARATION", "Préparation"),
                    new("HYGIENE", "Contrôle hygiène")
                },
                ["PLONGEUR"] = new List<CategoryOption>
                {
                    new("CONTROLE_VISUEL", "Contrôle visuel"),
                    new("NETTOYAGE", "Nettoyage"),
                    new("HYGIENE", "Contrôle hygiène")
                },

                // Logistique
                ["CARISTE"] = new List<CategoryOption>
                {
                    new("CONTROLE_VISUEL", "Contrôle pré-utilisation"),
                    new("VERIFICATION", "Vérification"),
                    new("MANUTENTION", "Manutention")
                },
                ["PREP_CMD"] = new List<CategoryOption>
                {
                    new("VERIFICATION", "Vérification"),
                    new("PREPARATION", "Préparation commande"),
                    new("CONTROLE", "Contrôle")
                },
                ["MAGASINIER"] = new List<CategoryOption>
                {
                    new("VERIFICATION", "Vérification"),
                    new("RECEPTION", "Réception"),
                    new("MESURE", "Inventaire"),
                    new("CONTROLE", "Contrôle")
                },
                ["AGENT_QUAI"] = new List<CategoryOption>
                {
                    new("VERIFICATION", "Vérification"),
                    new("CHARGEMENT", "Chargement/Déchargement"),
                    new("CONTROLE", "Contrôle")
                },

                // BTP
                ["MACON"] = new List<CategoryOption>
                {
                    new("CONTROLE_VISUEL", "Contrôle visuel"),
                    new("VERIFICATION", "Vérification"),
                    new("REALISATION", "Réalisation")
                },
                ["ELEC_BAT"] = new List<CategoryOption>
                {
                    new("VERIFICATION", "Vérification électrique"),
                    new("MESURE", "Mesures électriques"),
                    new("CONTROLE_REGLEMENTAIRE", "Contrôle réglementaire"),
                    new("INSTALLATION", "Installation")
                },
                ["PLOMBIER"] = new List<CategoryOption>
                {
                    new("VERIFICATION", "Vérification"),
                    new("TEST", "Test étanchéité"),
                    new("INSTALLATION", "Installation")
                },
                ["PEINTRE"] = new List<CategoryOption>
                {
                    new("CONTROLE_VISUEL", "Contrôle visuel"),
                    new("PREPARATION", "Préparation surface"),
                    new("REALISATION", "Réalisation")
                },
                ["CHARPENTIER"] = new List<CategoryOption>
                {
                    new("VERIFICATION", "Vérification"),
                    new("CONTROLE_VISUEL", "Contrôle visuel"),
                    new("ASSEMBLAGE", "Assemblage")
                },

                // IT
                ["TECH_SUPPORT"] = new List<CategoryOption>
                {
                    new("VERIFICATION", "Vérification"),
                    new("INTERVENTION", "Intervention"),
                    new("DIAGNOSTIC", "Diagnostic")
                },
                ["ADMIN_SYS"] = new List<CategoryOption>
                {
                    new("CONTROLE_VISUEL", "Contrôle visuel"),
                    new("VERIFICATION", "Vérification"),
                    new("INSPECTION", "Audit sécurité"),
                    new("SURVEILLANCE", "Surveillance")
                },
                ["DEV"] = new List<CategoryOption>
                {
                    new("VERIFICATION", "Revue de code"),
                    new("TEST", "Tests"),
                    new("DEVELOPPEMENT", "Développement")
                },
                ["TECH_RESEAU"] = new List<CategoryOption>
                {
                    new("VERIFICATION", "Vérification"),
                    new("CONTROLE", "Contrôle réseau"),
                    new("DIAGNOSTIC", "Diagnostic")
                }
            };
        }

        // Qualifications disponibles par métier
        public static Dictionary<string, List<QualificationOption>> GetQualificationsByIndustry()
        {
            return new Dictionary<string, List<QualificationOption>>
            {
                // QHSE
                ["RESP_QHSE"] = new List<QualificationOption>
                {
                    new("AUCUNE", "Aucune"),
                    new("INGENIEUR", "Ingénieur QHSE"),
                    new("RESP_QHSE", "Responsable QHSE")
                },
                ["ANIM_SEC"] = new List<QualificationOption>
                {
                    new("AUCUNE", "Aucune"),
                    new("ANIM_SEC", "Animateur sécurité"),
                    new("PREV_SECURITE", "Préventeur sécurité")
                },
                ["AUDIT_Q"] = new List<QualificationOption>
                {
                    new("AUCUNE", "Aucune"),
                    new("AUDITEUR_INTERNE", "Auditeur interne"),
                    new("TECH_QUALITE", "Technicien qualité")
                },
                ["TECH_HSE"] = new List<QualificationOption>
                {
                    new("AUCUNE", "Aucune"),
                    new("PREV_SECURITE", "Préventeur sécurité"),
                    new("TECH_HSE", "Technicien HSE")
                },

                // Santé
                ["AS"] = new List<QualificationOption>
                {
                    new("AIDE_SOIGNANT", "Aide-soignant diplômé"),
                    new("AUCUNE", "Aucune")
                },
                ["IDE"] = new List<QualificationOption>
                {
                    new("INFIRMIER", "Infirmier diplômé d'État")
                },
                ["AVS"] = new List<QualificationOption>
                {
                    new("AUCUNE", "Aucune"),
                    new("AVS", "Auxiliaire de vie")
                },
                ["ASH"] = new List<QualificationOption>
                {
                    new("AUCUNE", "Aucune")
                },

                // Nettoyage
                ["AGENT_NET"] = new List<QualificationOption>
                {
                    new("AUCUNE", "Aucune")
                },
                ["APU"] = new List<QualificationOption>
                {
                    new("AUCUNE", "Aucune")
                },
                ["CHEF_NET"] = new List<QualificationOption>
                {
                    new("AUCUNE", "Aucune"),
                    new("RESP_NETTOYAGE", "Responsable nettoyage")
                },

                // Sécurité
                ["ADS"] = new List<QualificationOption>
                {
                    new("AGENT_SECURITE", "Agent de sécurité (CQP APS)"),
                    new("AUCUNE", "Aucune")
                },
                ["CYNO"] = new List<QualificationOption>
                {
                    new("AGENT_SECURITE", "Agent de sécurité cynophile"),
                    new("CYNOPHILE", "Agent cynophile")
                },
                ["SSIAP"] = new List<QualificationOption>
                {
                    new("SSIAP", "SSIAP (1, 2 ou 3)"),
                    new("AGENT_SECURITE", "Agent de sécurité")
                },
                ["RONDIER"] = new List<QualificationOption>
                {
                    new("AUCUNE", "Aucune"),
                    new("AGENT_SECURITE", "Agent de sécurité")
                },

                // Commerce
                ["VENDEUR"] = new List<QualificationOption>
                {
                    new("AUCUNE", "Aucune")
                },
                ["CAISSIER"] = new List<QualificationOption>
                {
                    new("AUCUNE", "Aucune")
                },
                ["EMP_COM"] = new List<QualificationOption>
                {
                    new("AUCUNE", "Aucune")
                },
                ["CHEF_RAYON"] = new List<QualificationOption>
                {
                    new("AUCUNE", "Aucune"),
                    new("CHEF_RAYON", "Chef de rayon")
                },

                // Restauration
                ["CUISINIER"] = new List<QualificationOption>
                {
                    new("CUISINIER", "Cuisinier"),
                    new("AUCUNE", "Aucune")
                },
                ["SERVEUR"] = new List<QualificationOption>
                {
                    new("AUCUNE", "Aucune")
                },
                ["COMMIS"] = new List<QualificationOption>
                {
                    new("AUCUNE", "Aucune")
                },
                ["PLONGEUR"] = new List<QualificationOption>
                {
                    new("AUCUNE", "Aucune")
                },

                // Logistique
                ["CARISTE"] = new List<QualificationOption>
                {
                    new("CACES", "CACES (obligatoire)")
                },
                ["PREP_CMD"] = new List<QualificationOption>
                {
                    new("AUCUNE", "Aucune")
                },
                ["MAGASINIER"] = new List<QualificationOption>
                {
                    new("AUCUNE", "Aucune"),
                    new("MAGASINIER", "Magasinier")
                },
                ["AGENT_QUAI"] = new List<QualificationOption>
                {
                    new("AUCUNE", "Aucune")
                },

                // BTP
                ["MACON"] = new List<QualificationOption>
                {
                    new("MACON", "Maçon"),
                    new("AUCUNE", "Aucune")
                },
                ["ELEC_BAT"] = new List<QualificationOption>
                {
                    new("ELECTRICIEN", "Électricien"),
                    new("HABILITATION_ELECTRIQUE", "Habilitation électrique")
                },
                ["PLOMBIER"] = new List<QualificationOption>
                {
                    new("PLOMBIER", "Plombier"),
                    new("AUCUNE", "Aucune")
                },
                ["PEINTRE"] = new List<QualificationOption>
                {
                    new("AUCUNE", "Aucune"),
                    new("PEINTRE", "Peintre")
                },
                ["CHARPENTIER"] = new List<QualificationOption>
                {
                    new("CHARPENTIER", "Charpentier"),
                    new("AUCUNE", "Aucune")
                },

                // IT
                ["TECH_SUPPORT"] = new List<QualificationOption>
                {
                    new("TECH_IT", "Technicien IT"),
                    new("AUCUNE", "Aucune")
                },
                ["ADMIN_SYS"] = new List<QualificationOption>
                {
                    new("TECH_IT", "Technicien IT"),
                    new("ADMIN_SYSTEME", "Administrateur système")
                },
                ["DEV"] = new List<QualificationOption>
                {
                    new("DEV", "Développeur"),
                    new("AUCUNE", "Aucune")
                },
                ["TECH_RESEAU"] = new List<QualificationOption>
                {
                    new("TECH_RESEAU", "Technicien réseau"),
                    new("TECH_IT", "Technicien IT")
                }
            };
        }

        // Catégories communes à tous les métiers (fallback)
        public static List<CategoryOption> GetDefaultCategories()
        {
            return new List<CategoryOption>
            {
                new("CONTROLE_VISUEL", "Contrôle visuel"),
                new("VERIFICATION", "Vérification"),
                new("INSPECTION", "Inspection"),
                new("MESURE", "Mesure / Relevé")
            };
        }

        // Qualifications communes (fallback)
        public static List<QualificationOption> GetDefaultQualifications()
        {
            return new List<QualificationOption>
            {
                new("AUCUNE", "Aucune qualification requise")
            };
        }
    }

    public record CategoryOption(string Code, string Label);
    public record QualificationOption(string Code, string Label);
}
