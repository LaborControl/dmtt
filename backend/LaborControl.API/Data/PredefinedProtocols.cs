using LaborControl.API.Models;

namespace LaborControl.API.Data
{
    public static class PredefinedProtocols
    {
        public static List<TaskTemplate> GetPredefinedTaskTemplatesForIndustry(string industryCode, Guid customerId, Guid industryId)
        {
            var templates = new List<TaskTemplate>();

            switch (industryCode.ToUpper())
            {
                // ==================================================
                // QHSE
                // ==================================================
                case "RESP_QHSE":
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Audit QHSE mensuel", "INSPECTION", "INGENIEUR", customerId, industryId,
                            @"{""fields"":[{""name"":""conformite_generale"",""type"":""select"",""label"":""Conformité générale"",""options"":[""Conforme"",""Non conforme mineur"",""Non conforme majeur""],""required"":true},{""name"":""points_controles"",""type"":""text"",""label"":""Points de contrôle vérifiés"",""required"":true},{""name"":""actions_correctives"",""type"":""text"",""label"":""Actions correctives à mettre en place"",""required"":false}]}",
                            "Seul un responsable QHSE qualifié peut réaliser cet audit", true), // CRITIQUE: Double bornage requis
                        CreateTemplate("Contrôle conformité réglementaire", "CONTROLE_REGLEMENTAIRE", "RESP_QHSE", customerId, industryId,
                            @"{""fields"":[{""name"":""documents_verifies"",""type"":""text"",""label"":""Documents vérifiés"",""required"":true},{""name"":""ecarts_constates"",""type"":""text"",""label"":""Écarts constatés"",""required"":false},{""name"":""date_prochaine_verification"",""type"":""date"",""label"":""Prochaine vérification"",""required"":true}]}", null, true), // CRITIQUE: Double bornage requis
                        CreateTemplate("Inspection sécurité des lieux", "INSPECTION", "RESP_QHSE", customerId, industryId,
                            @"{""fields"":[{""name"":""zones_inspectees"",""type"":""text"",""label"":""Zones inspectées"",""required"":true},{""name"":""risques_identifies"",""type"":""text"",""label"":""Risques identifiés"",""required"":true},{""name"":""mesures_preventives"",""type"":""text"",""label"":""Mesures préventives"",""required"":true},{""name"":""photo"",""type"":""photo"",""label"":""Photos"",""required"":false}]}", null, true), // CRITIQUE: Double bornage requis
                    });
                    break;

                case "ANIM_SEC": // Animateur sécurité
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Animation sécurité équipe", "INSPECTION", "ANIM_SEC", customerId, industryId,
                            @"{""fields"":[{""name"":""equipe"",""type"":""text"",""label"":""Équipe"",""required"":true},{""name"":""theme_secu"",""type"":""text"",""label"":""Thème sécurité abordé"",""required"":true},{""name"":""points_amelioration"",""type"":""text"",""label"":""Points d'amélioration"",""required"":true}]}"),
                        CreateTemplate("Contrôle port des EPI", "CONTROLE_VISUEL", "AUCUNE", customerId, industryId,
                            @"{""fields"":[{""name"":""zone"",""type"":""text"",""label"":""Zone"",""required"":true},{""name"":""epi_conformes"",""type"":""select"",""label"":""EPI conformes"",""options"":[""Oui"",""Non""],""required"":true},{""name"":""observations"",""type"":""text"",""label"":""Observations"",""required"":false}]}"),
                    });
                    break;

                case "AUDIT_Q": // Auditeur qualité
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Audit interne qualité", "INSPECTION", "AUDITEUR_INTERNE", customerId, industryId,
                            @"{""fields"":[{""name"":""service_audite"",""type"":""text"",""label"":""Service audité"",""required"":true},{""name"":""points_forts"",""type"":""text"",""label"":""Points forts"",""required"":false},{""name"":""points_amelioration"",""type"":""text"",""label"":""Points d'amélioration"",""required"":true},{""name"":""plan_action"",""type"":""text"",""label"":""Plan d'action"",""required"":true}]}", null, true), // CRITIQUE: Double bornage requis
                        CreateTemplate("Contrôle qualité produit", "VERIFICATION", "TECH_QUALITE", customerId, industryId,
                            @"{""fields"":[{""name"":""ref_produit"",""type"":""text"",""label"":""Référence produit"",""required"":true},{""name"":""conformite"",""type"":""select"",""label"":""Conformité"",""options"":[""Conforme"",""Non conforme""],""required"":true},{""name"":""mesures"",""type"":""text"",""label"":""Mesures relevées"",""required"":true}]}", null, true), // CRITIQUE: Double bornage requis
                    });
                    break;

                case "TECH_HSE": // Technicien HSE
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Évaluation risques professionnels", "INSPECTION", "PREV_SECURITE", customerId, industryId,
                            @"{""fields"":[{""name"":""poste_travail"",""type"":""text"",""label"":""Poste de travail"",""required"":true},{""name"":""risques_identifies"",""type"":""text"",""label"":""Risques identifiés"",""required"":true},{""name"":""niveau_risque"",""type"":""select"",""label"":""Niveau de risque"",""options"":[""Faible"",""Moyen"",""Élevé"",""Critique""],""required"":true},{""name"":""mesures_prevention"",""type"":""text"",""label"":""Mesures de prévention"",""required"":true}]}", null, true), // CRITIQUE: Double bornage requis
                    });
                    break;

                // ==================================================
                // SANTÉ ET MÉDICO-SOCIAL
                // ==================================================
                case "AS": // Aide-soignant
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Contrôle hygiène chambre patient", "CONTROLE_VISUEL", "AIDE_SOIGNANT", customerId, industryId,
                            @"{""fields"":[{""name"":""numero_chambre"",""type"":""text"",""label"":""Numéro de chambre"",""required"":true},{""name"":""proprete"",""type"":""select"",""label"":""Propreté"",""options"":[""Excellente"",""Bonne"",""Acceptable"",""Insuffisante""],""required"":true},{""name"":""linge_change"",""type"":""select"",""label"":""Linge changé"",""options"":[""Oui"",""Non""],""required"":true}]}"),
                    });
                    break;

                case "IDE": // Infirmier
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Contrôle hygiène salle de soins", "CONTROLE_VISUEL", "INFIRMIER", customerId, industryId,
                            @"{""fields"":[{""name"":""proprete_generale"",""type"":""select"",""label"":""Propreté générale"",""options"":[""Excellente"",""Bonne"",""Acceptable"",""Insuffisante""],""required"":true},{""name"":""desinfection_surfaces"",""type"":""select"",""label"":""Désinfection des surfaces"",""options"":[""Conforme"",""Non conforme""],""required"":true},{""name"":""materiel_sterile"",""type"":""select"",""label"":""Matériel stérile disponible"",""options"":[""Oui"",""Non""],""required"":true}]}", null, true), // CRITIQUE: Risque infectieux - Double bornage requis
                        CreateTemplate("Vérification armoire à pharmacie", "VERIFICATION", "INFIRMIER", customerId, industryId,
                            @"{""fields"":[{""name"":""peremptions_verifiees"",""type"":""select"",""label"":""Dates de péremption vérifiées"",""options"":[""Oui"",""Non""],""required"":true},{""name"":""stocks_adequats"",""type"":""select"",""label"":""Stocks adéquats"",""options"":[""Oui"",""Non""],""required"":true},{""name"":""produits_a_commander"",""type"":""text"",""label"":""Produits à commander"",""required"":false}]}", null, true), // CRITIQUE: Gestion médicaments - Double bornage requis
                    });
                    break;

                case "AVS": // Auxiliaire de vie
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Contrôle sécurité domicile", "CONTROLE_VISUEL", "AUCUNE", customerId, industryId,
                            @"{""fields"":[{""name"":""risques_chute"",""type"":""text"",""label"":""Risques de chute identifiés"",""required"":true},{""name"":""equipements_secu"",""type"":""select"",""label"":""Équipements de sécurité"",""options"":[""Présents"",""Absents""],""required"":true},{""name"":""recommandations"",""type"":""text"",""label"":""Recommandations"",""required"":false}]}"),
                    });
                    break;

                case "ASH": // Agent de service hospitalier
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Nettoyage et désinfection chambre", "NETTOYAGE", "AUCUNE", customerId, industryId,
                            @"{""fields"":[{""name"":""numero_chambre"",""type"":""text"",""label"":""Numéro chambre"",""required"":true},{""name"":""surfaces_desinfectees"",""type"":""select"",""label"":""Surfaces désinfectées"",""options"":[""Oui"",""Non""],""required"":true},{""name"":""produit_utilise"",""type"":""text"",""label"":""Produit utilisé"",""required"":true}]}"),
                    });
                    break;

                // ==================================================
                // NETTOYAGE
                // ==================================================
                case "AGENT_NET": // Agent de nettoyage
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Nettoyage bureaux", "NETTOYAGE", "AUCUNE", customerId, industryId,
                            @"{""fields"":[{""name"":""zone"",""type"":""text"",""label"":""Zone nettoyée"",""required"":true},{""name"":""taches_effectuees"",""type"":""text"",""label"":""Tâches effectuées"",""required"":true},{""name"":""produits_utilises"",""type"":""text"",""label"":""Produits utilisés"",""required"":true},{""name"":""etat_final"",""type"":""select"",""label"":""État final"",""options"":[""Impeccable"",""Satisfaisant"",""À reprendre""],""required"":true}]}"),
                        CreateTemplate("Désinfection sanitaires", "NETTOYAGE", "AUCUNE", customerId, industryId,
                            @"{""fields"":[{""name"":""sanitaires"",""type"":""text"",""label"":""Sanitaires traités"",""required"":true},{""name"":""desinfection"",""type"":""select"",""label"":""Désinfection effectuée"",""options"":[""Oui"",""Non""],""required"":true},{""name"":""produit_desinfectant"",""type"":""text"",""label"":""Produit désinfectant"",""required"":true}]}"),
                    });
                    break;

                case "APU": // Agent de propreté urbaine
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Nettoyage voirie", "NETTOYAGE", "AUCUNE", customerId, industryId,
                            @"{""fields"":[{""name"":""secteur"",""type"":""text"",""label"":""Secteur"",""required"":true},{""name"":""type_dechets"",""type"":""text"",""label"":""Type de déchets collectés"",""required"":true},{""name"":""equipements_utilises"",""type"":""text"",""label"":""Équipements utilisés"",""required"":true}]}"),
                    });
                    break;

                case "CHEF_NET": // Chef d'équipe propreté
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Contrôle qualité nettoyage", "VERIFICATION", "RESP_NETTOYAGE", customerId, industryId,
                            @"{""fields"":[{""name"":""zones_controlees"",""type"":""text"",""label"":""Zones contrôlées"",""required"":true},{""name"":""conformite"",""type"":""select"",""label"":""Conformité"",""options"":[""Conforme"",""Non conforme""],""required"":true},{""name"":""actions_correctives"",""type"":""text"",""label"":""Actions correctives"",""required"":false},{""name"":""photo"",""type"":""photo"",""label"":""Photos"",""required"":false}]}"),
                    });
                    break;

                // ==================================================
                // SÉCURITÉ ET GARDIENNAGE
                // ==================================================
                case "ADS": // Agent de sécurité
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Ronde de sécurité", "CONTROLE_VISUEL", "AGENT_SECURITE", customerId, industryId,
                            @"{""fields"":[{""name"":""heure_ronde"",""type"":""text"",""label"":""Heure de la ronde"",""required"":true},{""name"":""zones_verifiees"",""type"":""text"",""label"":""Zones vérifiées"",""required"":true},{""name"":""anomalies"",""type"":""text"",""label"":""Anomalies détectées"",""required"":false},{""name"":""interventions"",""type"":""text"",""label"":""Interventions effectuées"",""required"":false}]}"),
                        CreateTemplate("Contrôle accès", "VERIFICATION", "AGENT_SECURITE", customerId, industryId,
                            @"{""fields"":[{""name"":""point_acces"",""type"":""text"",""label"":""Point d'accès"",""required"":true},{""name"":""badges_verifies"",""type"":""text"",""label"":""Nombre de badges vérifiés"",""required"":true},{""name"":""incidents"",""type"":""text"",""label"":""Incidents"",""required"":false}]}"),
                    });
                    break;

                case "CYNO": // Agent cynophile
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Ronde cynophile", "CONTROLE_VISUEL", "AGENT_SECURITE", customerId, industryId,
                            @"{""fields"":[{""name"":""secteur"",""type"":""text"",""label"":""Secteur patrouillé"",""required"":true},{""name"":""alertes_chien"",""type"":""text"",""label"":""Alertes du chien"",""required"":false},{""name"":""anomalies"",""type"":""text"",""label"":""Anomalies détectées"",""required"":false}]}"),
                    });
                    break;

                case "SSIAP": // Agent de sécurité incendie
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Contrôle équipements incendie", "VERIFICATION", "SSIAP", customerId, industryId,
                            @"{""fields"":[{""name"":""equipement"",""type"":""text"",""label"":""Équipement contrôlé"",""required"":true},{""name"":""fonctionnement"",""type"":""select"",""label"":""Fonctionnement"",""options"":[""OK"",""Défaillant""],""required"":true},{""name"":""date_prochaine_verification"",""type"":""date"",""label"":""Prochaine vérification"",""required"":true}]}",
                            "Contrôle réglementaire obligatoire"),
                    });
                    break;

                case "RONDIER": // Rondier-intervenant
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Ronde d'intervention", "CONTROLE_VISUEL", "AUCUNE", customerId, industryId,
                            @"{""fields"":[{""name"":""circuit"",""type"":""text"",""label"":""Circuit effectué"",""required"":true},{""name"":""points_controle"",""type"":""text"",""label"":""Points de contrôle vérifiés"",""required"":true},{""name"":""anomalies"",""type"":""text"",""label"":""Anomalies"",""required"":false}]}"),
                    });
                    break;

                // ==================================================
                // COMMERCE
                // ==================================================
                case "VENDEUR":
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Contrôle merchandising", "CONTROLE_VISUEL", "AUCUNE", customerId, industryId,
                            @"{""fields"":[{""name"":""rayon"",""type"":""text"",""label"":""Rayon"",""required"":true},{""name"":""facing"",""type"":""select"",""label"":""Facing correct"",""options"":[""Oui"",""Non""],""required"":true},{""name"":""balisage_prix"",""type"":""select"",""label"":""Balisage prix"",""options"":[""Conforme"",""Non conforme""],""required"":true},{""name"":""ruptures"",""type"":""text"",""label"":""Ruptures détectées"",""required"":false}]}"),
                        CreateTemplate("Inventaire rayon", "MESURE", "AUCUNE", customerId, industryId,
                            @"{""fields"":[{""name"":""rayon"",""type"":""text"",""label"":""Rayon"",""required"":true},{""name"":""articles_comptes"",""type"":""text"",""label"":""Nombre d'articles comptés"",""required"":true},{""name"":""ecarts"",""type"":""text"",""label"":""Écarts constatés"",""required"":false}]}"),
                    });
                    break;

                case "CAISSIER": // Caissier
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Contrôle caisse", "VERIFICATION", "AUCUNE", customerId, industryId,
                            @"{""fields"":[{""name"":""numero_caisse"",""type"":""text"",""label"":""Numéro de caisse"",""required"":true},{""name"":""montant_theorique"",""type"":""text"",""label"":""Montant théorique"",""required"":true},{""name"":""montant_reel"",""type"":""text"",""label"":""Montant réel"",""required"":true},{""name"":""ecart"",""type"":""text"",""label"":""Écart"",""required"":false}]}"),
                    });
                    break;

                case "EMP_COM": // Employé commercial
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Mise en rayon", "CONTROLE_VISUEL", "AUCUNE", customerId, industryId,
                            @"{""fields"":[{""name"":""rayon"",""type"":""text"",""label"":""Rayon"",""required"":true},{""name"":""produits_ranges"",""type"":""text"",""label"":""Produits rangés"",""required"":true},{""name"":""rotation_stock"",""type"":""select"",""label"":""Rotation FIFO respectée"",""options"":[""Oui"",""Non""],""required"":true}]}"),
                    });
                    break;

                case "CHEF_RAYON":
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Contrôle dates de péremption", "VERIFICATION", "CHEF_RAYON", customerId, industryId,
                            @"{""fields"":[{""name"":""rayon"",""type"":""text"",""label"":""Rayon"",""required"":true},{""name"":""produits_retires"",""type"":""text"",""label"":""Produits retirés"",""required"":false},{""name"":""prochaine_verification"",""type"":""date"",""label"":""Prochaine vérification"",""required"":true}]}"),
                    });
                    break;

                // ==================================================
                // RESTAURATION
                // ==================================================
                case "CUISINIER":
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Contrôle températures cuisine", "MESURE", "CUISINIER", customerId, industryId,
                            @"{""fields"":[{""name"":""temp_frigo"",""type"":""text"",""label"":""Température frigo (°C)"",""required"":true},{""name"":""temp_congelateur"",""type"":""text"",""label"":""Température congélateur (°C)"",""required"":true},{""name"":""temp_plats_chauds"",""type"":""text"",""label"":""Température plats chauds (°C)"",""required"":true},{""name"":""conformite"",""type"":""select"",""label"":""Conformité"",""options"":[""Conforme"",""Non conforme""],""required"":true}]}",
                            "Contrôle réglementaire obligatoire - Respecter la chaîne du froid"),
                        CreateTemplate("Nettoyage poste de travail cuisine", "NETTOYAGE", "CUISINIER", customerId, industryId,
                            @"{""fields"":[{""name"":""poste"",""type"":""text"",""label"":""Poste de travail"",""required"":true},{""name"":""desinfection"",""type"":""select"",""label"":""Désinfection effectuée"",""options"":[""Oui"",""Non""],""required"":true},{""name"":""produit_utilise"",""type"":""text"",""label"":""Produit utilisé"",""required"":true}]}"),
                    });
                    break;

                case "SERVEUR":
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Contrôle mise en place salle", "CONTROLE_VISUEL", "AUCUNE", customerId, industryId,
                            @"{""fields"":[{""name"":""zone"",""type"":""text"",""label"":""Zone"",""required"":true},{""name"":""tables_dressees"",""type"":""select"",""label"":""Tables dressées"",""options"":[""Oui"",""Non""],""required"":true},{""name"":""proprete"",""type"":""select"",""label"":""Propreté"",""options"":[""Impeccable"",""Satisfaisant"",""À reprendre""],""required"":true}]}"),
                    });
                    break;

                case "COMMIS": // Commis de cuisine
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Préparation ingrédients", "CONTROLE_VISUEL", "AUCUNE", customerId, industryId,
                            @"{""fields"":[{""name"":""ingredient"",""type"":""text"",""label"":""Ingrédient préparé"",""required"":true},{""name"":""quantite"",""type"":""text"",""label"":""Quantité"",""required"":true},{""name"":""fraicheur"",""type"":""select"",""label"":""Fraîcheur"",""options"":[""Excellent"",""Bon"",""À surveiller""],""required"":true}]}"),
                    });
                    break;

                case "PLONGEUR":
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Contrôle propreté vaisselle", "CONTROLE_VISUEL", "AUCUNE", customerId, industryId,
                            @"{""fields"":[{""name"":""type_vaisselle"",""type"":""text"",""label"":""Type de vaisselle"",""required"":true},{""name"":""proprete"",""type"":""select"",""label"":""Propreté"",""options"":[""Impeccable"",""À reprendre""],""required"":true},{""name"":""temperature_eau"",""type"":""text"",""label"":""Température eau (°C)"",""required"":true}]}"),
                    });
                    break;

                // ==================================================
                // LOGISTIQUE
                // ==================================================
                case "CARISTE":
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Contrôle pré-utilisation chariot", "CONTROLE_VISUEL", "CACES", customerId, industryId,
                            @"{""fields"":[{""name"":""numero_chariot"",""type"":""text"",""label"":""Numéro chariot"",""required"":true},{""name"":""freins"",""type"":""select"",""label"":""Freins"",""options"":[""OK"",""Défaillant""],""required"":true},{""name"":""klaxon"",""type"":""select"",""label"":""Klaxon"",""options"":[""OK"",""Défaillant""],""required"":true},{""name"":""fourches"",""type"":""select"",""label"":""Fourches"",""options"":[""OK"",""Défaillant""],""required"":true},{""name"":""observations"",""type"":""text"",""label"":""Observations"",""required"":false}]}",
                            "CACES obligatoire - Contrôle quotidien réglementaire"),
                    });
                    break;

                case "PREP_CMD": // Préparateur de commandes
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Préparation commande", "VERIFICATION", "AUCUNE", customerId, industryId,
                            @"{""fields"":[{""name"":""numero_commande"",""type"":""text"",""label"":""Numéro commande"",""required"":true},{""name"":""articles_prepares"",""type"":""text"",""label"":""Nombre d'articles"",""required"":true},{""name"":""controle_quantite"",""type"":""select"",""label"":""Quantités conformes"",""options"":[""Oui"",""Non""],""required"":true}]}"),
                    });
                    break;

                case "MAGASINIER":
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Contrôle réception marchandises", "VERIFICATION", "AUCUNE", customerId, industryId,
                            @"{""fields"":[{""name"":""numero_bl"",""type"":""text"",""label"":""Numéro BL"",""required"":true},{""name"":""quantite_conforme"",""type"":""select"",""label"":""Quantité conforme"",""options"":[""Oui"",""Non""],""required"":true},{""name"":""etat_marchandise"",""type"":""select"",""label"":""État marchandise"",""options"":[""Bon état"",""Endommagé""],""required"":true}]}"),
                        CreateTemplate("Inventaire zone stockage", "MESURE", "MAGASINIER", customerId, industryId,
                            @"{""fields"":[{""name"":""zone"",""type"":""text"",""label"":""Zone"",""required"":true},{""name"":""references_comptees"",""type"":""text"",""label"":""Références comptées"",""required"":true},{""name"":""ecarts"",""type"":""text"",""label"":""Écarts"",""required"":false}]}"),
                    });
                    break;

                case "AGENT_QUAI": // Agent de quai
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Contrôle chargement/déchargement", "VERIFICATION", "AUCUNE", customerId, industryId,
                            @"{""fields"":[{""name"":""camion"",""type"":""text"",""label"":""Immatriculation camion"",""required"":true},{""name"":""nb_palettes"",""type"":""text"",""label"":""Nombre de palettes"",""required"":true},{""name"":""etat_marchandise"",""type"":""select"",""label"":""État marchandise"",""options"":[""Bon état"",""Endommagé""],""required"":true}]}"),
                    });
                    break;

                // ==================================================
                // BTP
                // ==================================================
                case "MACON":
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Contrôle échafaudage", "CONTROLE_VISUEL", "MACON", customerId, industryId,
                            @"{""fields"":[{""name"":""stabilite"",""type"":""select"",""label"":""Stabilité"",""options"":[""Stable"",""Instable""],""required"":true},{""name"":""garde_corps"",""type"":""select"",""label"":""Garde-corps"",""options"":[""Présents"",""Absents""],""required"":true},{""name"":""plinthes"",""type"":""select"",""label"":""Plinthes"",""options"":[""Présentes"",""Absentes""],""required"":true}]}",
                            "Contrôle obligatoire avant utilisation"),
                    });
                    break;

                case "ELEC_BAT": // Électricien bâtiment
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Contrôle installation électrique", "VERIFICATION", "ELECTRICIEN", customerId, industryId,
                            @"{""fields"":[{""name"":""zone"",""type"":""text"",""label"":""Zone"",""required"":true},{""name"":""tension"",""type"":""text"",""label"":""Tension mesurée (V)"",""required"":true},{""name"":""terre"",""type"":""select"",""label"":""Mise à la terre"",""options"":[""Conforme"",""Non conforme""],""required"":true}]}"),
                    });
                    break;

                case "PLOMBIER":
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Test étanchéité", "VERIFICATION", "PLOMBIER", customerId, industryId,
                            @"{""fields"":[{""name"":""installation"",""type"":""text"",""label"":""Installation testée"",""required"":true},{""name"":""pression"",""type"":""text"",""label"":""Pression (bar)"",""required"":true},{""name"":""etancheite"",""type"":""select"",""label"":""Étanchéité"",""options"":[""OK"",""Fuite détectée""],""required"":true}]}"),
                    });
                    break;

                case "PEINTRE": // Peintre en bâtiment
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Contrôle préparation surface", "CONTROLE_VISUEL", "AUCUNE", customerId, industryId,
                            @"{""fields"":[{""name"":""surface"",""type"":""text"",""label"":""Surface préparée"",""required"":true},{""name"":""etat_surface"",""type"":""select"",""label"":""État surface"",""options"":[""Prête"",""À reprendre""],""required"":true},{""name"":""type_peinture"",""type"":""text"",""label"":""Type de peinture"",""required"":true}]}"),
                    });
                    break;

                case "CHARPENTIER":
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Contrôle assemblage charpente", "VERIFICATION", "CHARPENTIER", customerId, industryId,
                            @"{""fields"":[{""name"":""element"",""type"":""text"",""label"":""Élément contrôlé"",""required"":true},{""name"":""assemblage"",""type"":""select"",""label"":""Assemblage"",""options"":[""Conforme"",""À reprendre""],""required"":true},{""name"":""niveau"",""type"":""select"",""label"":""Niveau/Aplomb"",""options"":[""OK"",""Ajustement nécessaire""],""required"":true}]}"),
                    });
                    break;

                // ==================================================
                // IT / INFORMATIQUE
                // ==================================================
                case "TECH_SUPPORT": // Technicien support
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Intervention support utilisateur", "VERIFICATION", "TECH_IT", customerId, industryId,
                            @"{""fields"":[{""name"":""ticket"",""type"":""text"",""label"":""Numéro ticket"",""required"":true},{""name"":""probleme"",""type"":""text"",""label"":""Problème signalé"",""required"":true},{""name"":""resolution"",""type"":""text"",""label"":""Solution appliquée"",""required"":true},{""name"":""statut"",""type"":""select"",""label"":""Statut"",""options"":[""Résolu"",""En cours"",""Escaladé""],""required"":true}]}"),
                    });
                    break;

                case "ADMIN_SYS": // Administrateur système
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Contrôle salle serveurs", "CONTROLE_VISUEL", "TECH_IT", customerId, industryId,
                            @"{""fields"":[{""name"":""temperature"",""type"":""text"",""label"":""Température (°C)"",""required"":true},{""name"":""humidite"",""type"":""text"",""label"":""Humidité (%)"",""required"":true},{""name"":""climatisation"",""type"":""select"",""label"":""Climatisation"",""options"":[""OK"",""Défaillant""],""required"":true},{""name"":""onduleurs"",""type"":""select"",""label"":""Onduleurs"",""options"":[""OK"",""Alerte""],""required"":true}]}"),
                        CreateTemplate("Vérification sauvegardes", "VERIFICATION", "TECH_IT", customerId, industryId,
                            @"{""fields"":[{""name"":""derniere_sauvegarde"",""type"":""date"",""label"":""Dernière sauvegarde"",""required"":true},{""name"":""statut"",""type"":""select"",""label"":""Statut"",""options"":[""Réussie"",""Échec"",""Partielle""],""required"":true},{""name"":""espace_disponible"",""type"":""text"",""label"":""Espace disponible"",""required"":true}]}"),
                        CreateTemplate("Audit sécurité système", "INSPECTION", "ADMIN_SYSTEME", customerId, industryId,
                            @"{""fields"":[{""name"":""systeme"",""type"":""text"",""label"":""Système audité"",""required"":true},{""name"":""mises_a_jour"",""type"":""select"",""label"":""Mises à jour"",""options"":[""À jour"",""Retard""],""required"":true},{""name"":""vulnerabilites"",""type"":""text"",""label"":""Vulnérabilités détectées"",""required"":false},{""name"":""actions_correctives"",""type"":""text"",""label"":""Actions correctives"",""required"":true}]}"),
                    });
                    break;

                case "DEV": // Développeur
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Revue de code", "VERIFICATION", "DEV", customerId, industryId,
                            @"{""fields"":[{""name"":""module"",""type"":""text"",""label"":""Module"",""required"":true},{""name"":""qualite_code"",""type"":""select"",""label"":""Qualité du code"",""options"":[""Excellent"",""Bon"",""À améliorer""],""required"":true},{""name"":""tests_unitaires"",""type"":""select"",""label"":""Tests unitaires"",""options"":[""Présents"",""Absents"",""Incomplets""],""required"":true},{""name"":""commentaires"",""type"":""text"",""label"":""Commentaires"",""required"":false}]}"),
                    });
                    break;

                case "TECH_RESEAU": // Technicien réseau
                    templates.AddRange(new[]
                    {
                        CreateTemplate("Contrôle équipements réseau", "VERIFICATION", "TECH_RESEAU", customerId, industryId,
                            @"{""fields"":[{""name"":""equipement"",""type"":""text"",""label"":""Équipement"",""required"":true},{""name"":""connexion"",""type"":""select"",""label"":""Connexion"",""options"":[""OK"",""Instable"",""Hors ligne""],""required"":true},{""name"":""bande_passante"",""type"":""text"",""label"":""Bande passante"",""required"":false},{""name"":""observations"",""type"":""text"",""label"":""Observations"",""required"":false}]}"),
                    });
                    break;

                default:
                    // Protocole générique pour métiers non spécifiés
                    templates.Add(CreateTemplate("Contrôle visuel général", "CONTROLE_VISUEL", "AUCUNE", customerId, industryId,
                        @"{""fields"":[{""name"":""zone"",""type"":""text"",""label"":""Zone contrôlée"",""required"":true},{""name"":""etat"",""type"":""select"",""label"":""État"",""options"":[""Bon"",""Acceptable"",""Mauvais""],""required"":true},{""name"":""observations"",""type"":""text"",""label"":""Observations"",""required"":false}]}"));
                    break;
            }

            return templates;
        }

        private static TaskTemplate CreateTemplate(string name, string category, string requiredQualification, Guid customerId, Guid industryId, string formTemplate, string? legalWarning = null, bool requireDoubleScan = false)
        {
            return new TaskTemplate
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                IndustryId = industryId,
                Name = name,
                Category = category,
                FormTemplate = formTemplate,
                LegalWarning = legalWarning,
                RequireDoubleScan = requireDoubleScan, // NOUVEAU: Double bornage NFC
                IsPredefined = true,
                IsActive = false, // Inactive by default
                IsUniversal = false,
                AlertOnMismatch = true,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
