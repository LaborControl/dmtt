using System.Text.Json;
using System.Text.Json.Serialization;
using LaborControl.API.Models;

namespace LaborControl.API.Services
{
    /// <summary>
    /// Service d'intégration avec les données France Compétences (RNCP et RS)
    /// Les données sont disponibles via data.gouv.fr en format XML/CSV
    /// </summary>
    public interface IFranceCompetencesService
    {
        Task<FranceCompetencesSearchResult> SearchRncpAsync(string query, int page = 1, int pageSize = 20);
        Task<FranceCompetencesSearchResult> SearchRsAsync(string query, int page = 1, int pageSize = 20);
        Task<RncpCertificationInfo?> GetRncpDetailsAsync(string rncpCode);
        Task<RsCertificationInfo?> GetRsDetailsAsync(string rsCode);
    }

    public class FranceCompetencesService : IFranceCompetencesService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FranceCompetencesService> _logger;

        // API France Compétences via data.gouv.fr (données ouvertes)
        private const string RNCP_API_BASE = "https://www.data.gouv.fr/api/1/datasets/rncp-repertoire-national-des-certifications-professionnelles";
        private const string RS_API_BASE = "https://www.data.gouv.fr/api/1/datasets/rs-repertoire-specifique";

        // Pour le parsing des données RNCP/RS, on utilisera les fichiers CSV/XML disponibles
        // Note: France Compétences ne fournit pas d'API REST directe, donc on parse les fichiers de données

        public FranceCompetencesService(HttpClient httpClient, ILogger<FranceCompetencesService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Recherche dans le répertoire RNCP
        /// Note: Cette implémentation utilise une recherche locale simulée.
        /// En production, il faudrait indexer les données CSV/XML de data.gouv.fr
        /// </summary>
        public async Task<FranceCompetencesSearchResult> SearchRncpAsync(string query, int page = 1, int pageSize = 20)
        {
            try
            {
                _logger.LogInformation("Recherche RNCP: {Query}", query);

                // Simulation - En production, indexer les données de data.gouv.fr
                // Pour l'instant, retourner des résultats basés sur des certifications communes

                var results = GetCommonRncpCertifications()
                    .Where(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                (c.RncpCode != null && c.RncpCode.Contains(query, StringComparison.OrdinalIgnoreCase)))
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return new FranceCompetencesSearchResult
                {
                    Results = results,
                    TotalCount = results.Count,
                    Page = page,
                    PageSize = pageSize,
                    Source = "RNCP - Répertoire National des Certifications Professionnelles"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche RNCP");
                return new FranceCompetencesSearchResult
                {
                    Results = new List<CertificationSearchItem>(),
                    TotalCount = 0,
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// Recherche dans le répertoire spécifique (RS)
        /// </summary>
        public async Task<FranceCompetencesSearchResult> SearchRsAsync(string query, int page = 1, int pageSize = 20)
        {
            try
            {
                _logger.LogInformation("Recherche RS: {Query}", query);

                var results = GetCommonRsCertifications()
                    .Where(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                (c.RsCode != null && c.RsCode.Contains(query, StringComparison.OrdinalIgnoreCase)))
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return new FranceCompetencesSearchResult
                {
                    Results = results,
                    TotalCount = results.Count,
                    Page = page,
                    PageSize = pageSize,
                    Source = "RS - Répertoire Spécifique"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche RS");
                return new FranceCompetencesSearchResult
                {
                    Results = new List<CertificationSearchItem>(),
                    TotalCount = 0,
                    Error = ex.Message
                };
            }
        }

        public async Task<RncpCertificationInfo?> GetRncpDetailsAsync(string rncpCode)
        {
            try
            {
                var cert = GetCommonRncpCertifications()
                    .FirstOrDefault(c => c.RncpCode == rncpCode);

                if (cert == null) return null;

                return new RncpCertificationInfo
                {
                    RncpCode = cert.RncpCode,
                    Name = cert.Name,
                    Level = cert.Level,
                    Certificateur = cert.Certificateur,
                    DateEnregistrement = cert.DateEnregistrement,
                    DateFinValidite = cert.DateFinValidite,
                    FranceCompetencesUrl = $"https://www.francecompetences.fr/recherche/rncp/{cert.RncpCode.Replace("RNCP", "")}",
                    Description = cert.Description,
                    Secteurs = cert.Secteurs
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des détails RNCP {Code}", rncpCode);
                return null;
            }
        }

        public async Task<RsCertificationInfo?> GetRsDetailsAsync(string rsCode)
        {
            try
            {
                var cert = GetCommonRsCertifications()
                    .FirstOrDefault(c => c.RsCode == rsCode);

                if (cert == null) return null;

                return new RsCertificationInfo
                {
                    RsCode = cert.RsCode,
                    Name = cert.Name,
                    Certificateur = cert.Certificateur,
                    DateEnregistrement = cert.DateEnregistrement,
                    DateFinValidite = cert.DateFinValidite,
                    FranceCompetencesUrl = $"https://www.francecompetences.fr/recherche/rs/{cert.RsCode.Replace("RS", "")}",
                    Description = cert.Description
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des détails RS {Code}", rsCode);
                return null;
            }
        }

        /// <summary>
        /// Liste de certifications RNCP courantes pour les secteurs ciblés
        /// En production, ces données proviendraient d'une indexation des fichiers data.gouv.fr
        /// </summary>
        private List<CertificationSearchItem> GetCommonRncpCertifications()
        {
            return new List<CertificationSearchItem>
            {
                // QHSE
                new CertificationSearchItem
                {
                    RncpCode = "RNCP36368",
                    Name = "Manager en santé, sécurité et environnement au travail",
                    Level = 7,
                    Certificateur = "Institut de formation en management des risques",
                    DateEnregistrement = new DateTime(2022, 3, 25),
                    DateFinValidite = new DateTime(2027, 3, 25),
                    Description = "Formation aux métiers de la prévention des risques professionnels",
                    Secteurs = new List<string> { "QHSE" }
                },
                new CertificationSearchItem
                {
                    RncpCode = "RNCP36610",
                    Name = "Responsable qualité hygiène sécurité environnement",
                    Level = 6,
                    Certificateur = "CNAM",
                    DateEnregistrement = new DateTime(2022, 7, 1),
                    DateFinValidite = new DateTime(2027, 7, 1),
                    Description = "Responsable QHSE en entreprise industrielle ou tertiaire",
                    Secteurs = new List<string> { "QHSE" }
                },
                // Maintenance industrielle
                new CertificationSearchItem
                {
                    RncpCode = "RNCP35191",
                    Name = "Technicien supérieur en maintenance industrielle",
                    Level = 5,
                    Certificateur = "Ministère de l'Enseignement supérieur",
                    DateEnregistrement = new DateTime(2020, 11, 30),
                    DateFinValidite = new DateTime(2025, 11, 30),
                    Description = "BTS Maintenance des systèmes option systèmes de production",
                    Secteurs = new List<string> { "MAINTENANCE" }
                },
                new CertificationSearchItem
                {
                    RncpCode = "RNCP36281",
                    Name = "Ingénieur maintenance industrielle",
                    Level = 7,
                    Certificateur = "ENSAM",
                    DateEnregistrement = new DateTime(2022, 2, 25),
                    DateFinValidite = new DateTime(2027, 2, 25),
                    Description = "Ingénieur spécialisé en maintenance et fiabilité des équipements",
                    Secteurs = new List<string> { "MAINTENANCE" }
                },
                // Santé et médico-social
                new CertificationSearchItem
                {
                    RncpCode = "RNCP4495",
                    Name = "Diplôme d'État d'aide-soignant",
                    Level = 4,
                    Certificateur = "Ministère de la Santé",
                    DateEnregistrement = new DateTime(2021, 6, 10),
                    DateFinValidite = new DateTime(2026, 6, 10),
                    Description = "Aide-soignant en établissement de santé",
                    Secteurs = new List<string> { "SANTE" }
                },
                new CertificationSearchItem
                {
                    RncpCode = "RNCP492",
                    Name = "Diplôme d'État d'infirmier",
                    Level = 6,
                    Certificateur = "Ministère de la Santé",
                    DateEnregistrement = new DateTime(2020, 1, 1),
                    DateFinValidite = null,
                    Description = "Infirmier diplômé d'État",
                    Secteurs = new List<string> { "SANTE" }
                },
                // Nettoyage et propreté
                new CertificationSearchItem
                {
                    RncpCode = "RNCP35750",
                    Name = "Agent de propreté et d'hygiène",
                    Level = 3,
                    Certificateur = "Ministère du Travail",
                    DateEnregistrement = new DateTime(2021, 5, 21),
                    DateFinValidite = new DateTime(2026, 5, 21),
                    Description = "Agent de nettoyage en milieu professionnel",
                    Secteurs = new List<string> { "NETTOYAGE" }
                },
                new CertificationSearchItem
                {
                    RncpCode = "RNCP35183",
                    Name = "Responsable d'unité de propreté",
                    Level = 5,
                    Certificateur = "INHNI",
                    DateEnregistrement = new DateTime(2020, 11, 30),
                    DateFinValidite = new DateTime(2025, 11, 30),
                    Description = "Manager d'équipes de nettoyage industriel",
                    Secteurs = new List<string> { "NETTOYAGE" }
                },
                // Sécurité et gardiennage
                new CertificationSearchItem
                {
                    RncpCode = "RNCP34507",
                    Name = "Agent de prévention et de sécurité",
                    Level = 3,
                    Certificateur = "Ministère du Travail",
                    DateEnregistrement = new DateTime(2020, 2, 21),
                    DateFinValidite = new DateTime(2025, 2, 21),
                    Description = "Agent de sécurité privée CQP APS",
                    Secteurs = new List<string> { "SECURITE" }
                },
                // Logistique
                new CertificationSearchItem
                {
                    RncpCode = "RNCP35869",
                    Name = "Responsable logistique",
                    Level = 6,
                    Certificateur = "AFTRAL",
                    DateEnregistrement = new DateTime(2021, 7, 8),
                    DateFinValidite = new DateTime(2026, 7, 8),
                    Description = "Manager de la supply chain et de la logistique",
                    Secteurs = new List<string> { "LOGISTIQUE" }
                },
                new CertificationSearchItem
                {
                    RncpCode = "RNCP36375",
                    Name = "Préparateur de commandes en entrepôt",
                    Level = 3,
                    Certificateur = "Ministère du Travail",
                    DateEnregistrement = new DateTime(2022, 3, 25),
                    DateFinValidite = new DateTime(2027, 3, 25),
                    Description = "Préparateur de commandes avec CACES 1 et 3",
                    Secteurs = new List<string> { "LOGISTIQUE" }
                },
                // BTP
                new CertificationSearchItem
                {
                    RncpCode = "RNCP35315",
                    Name = "Conducteur de travaux du bâtiment et du génie civil",
                    Level = 6,
                    Certificateur = "ESTP",
                    DateEnregistrement = new DateTime(2021, 2, 10),
                    DateFinValidite = new DateTime(2026, 2, 10),
                    Description = "Conducteur de travaux en construction",
                    Secteurs = new List<string> { "BTP" }
                },
                // IT
                new CertificationSearchItem
                {
                    RncpCode = "RNCP36137",
                    Name = "Administrateur d'infrastructures sécurisées",
                    Level = 6,
                    Certificateur = "Ministère du Travail",
                    DateEnregistrement = new DateTime(2021, 12, 1),
                    DateFinValidite = new DateTime(2026, 12, 1),
                    Description = "Administrateur systèmes et réseaux",
                    Secteurs = new List<string> { "IT" }
                }
            };
        }

        /// <summary>
        /// Liste de certifications du Répertoire Spécifique (habilitations, certifications transversales)
        /// </summary>
        private List<CertificationSearchItem> GetCommonRsCertifications()
        {
            return new List<CertificationSearchItem>
            {
                // Habilitations électriques
                new CertificationSearchItem
                {
                    RsCode = "RS6401",
                    Name = "Habilitation électrique B1V-BR",
                    Certificateur = "INRS",
                    DateEnregistrement = new DateTime(2023, 1, 1),
                    DateFinValidite = new DateTime(2028, 1, 1),
                    Description = "Habilitation pour travaux d'ordre électrique en basse tension"
                },
                new CertificationSearchItem
                {
                    RsCode = "RS6402",
                    Name = "Habilitation électrique H0-H0V",
                    Certificateur = "INRS",
                    DateEnregistrement = new DateTime(2023, 1, 1),
                    DateFinValidite = new DateTime(2028, 1, 1),
                    Description = "Habilitation pour travaux d'ordre non électrique en haute tension"
                },
                // CACES
                new CertificationSearchItem
                {
                    RsCode = "RS5414",
                    Name = "CACES R489 - Chariots de manutention automoteurs",
                    Certificateur = "CNAMTS",
                    DateEnregistrement = new DateTime(2020, 1, 1),
                    DateFinValidite = new DateTime(2025, 1, 1),
                    Description = "Conduite en sécurité des chariots élévateurs"
                },
                new CertificationSearchItem
                {
                    RsCode = "RS5424",
                    Name = "CACES R486 - Plateformes élévatrices mobiles de personnel (PEMP)",
                    Certificateur = "CNAMTS",
                    DateEnregistrement = new DateTime(2020, 1, 1),
                    DateFinValidite = new DateTime(2025, 1, 1),
                    Description = "Conduite en sécurité des nacelles élévatrices"
                },
                new CertificationSearchItem
                {
                    RsCode = "RS5433",
                    Name = "CACES R482 - Engins de chantier",
                    Certificateur = "CNAMTS",
                    DateEnregistrement = new DateTime(2020, 1, 1),
                    DateFinValidite = new DateTime(2025, 1, 1),
                    Description = "Conduite en sécurité des engins de chantier"
                },
                // SST
                new CertificationSearchItem
                {
                    RsCode = "RS5226",
                    Name = "Sauveteur secouriste du travail (SST)",
                    Certificateur = "INRS",
                    DateEnregistrement = new DateTime(2019, 12, 18),
                    DateFinValidite = new DateTime(2024, 12, 18),
                    Description = "Formation aux premiers secours en entreprise"
                },
                // Sécurité
                new CertificationSearchItem
                {
                    RsCode = "RS5748",
                    Name = "SSIAP 1 - Service de sécurité incendie et d'assistance aux personnes",
                    Certificateur = "Ministère de l'Intérieur",
                    DateEnregistrement = new DateTime(2021, 3, 10),
                    DateFinValidite = new DateTime(2026, 3, 10),
                    Description = "Agent de sécurité incendie en ERP et IGH"
                },
                new CertificationSearchItem
                {
                    RsCode = "RS5749",
                    Name = "SSIAP 2 - Chef d'équipe de sécurité incendie",
                    Certificateur = "Ministère de l'Intérieur",
                    DateEnregistrement = new DateTime(2021, 3, 10),
                    DateFinValidite = new DateTime(2026, 3, 10),
                    Description = "Chef d'équipe de sécurité incendie"
                },
                // Hygiène alimentaire
                new CertificationSearchItem
                {
                    RsCode = "RS6128",
                    Name = "Bonnes pratiques d'hygiène en restauration commerciale",
                    Certificateur = "Ministère de l'Agriculture",
                    DateEnregistrement = new DateTime(2022, 9, 29),
                    DateFinValidite = new DateTime(2027, 9, 29),
                    Description = "Formation HACCP obligatoire en restauration"
                },
                // Travail en hauteur
                new CertificationSearchItem
                {
                    RsCode = "RS5512",
                    Name = "Travail en hauteur - Utilisation des équipements de protection individuelle contre les chutes",
                    Certificateur = "OPPBTP",
                    DateEnregistrement = new DateTime(2020, 4, 30),
                    DateFinValidite = new DateTime(2025, 4, 30),
                    Description = "Formation travail en hauteur avec harnais et ligne de vie"
                }
            };
        }
    }

    // DTOs pour les réponses
    public class FranceCompetencesSearchResult
    {
        public List<CertificationSearchItem> Results { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Source { get; set; }
        public string? Error { get; set; }
    }

    public class CertificationSearchItem
    {
        public string? RncpCode { get; set; }
        public string? RsCode { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? Level { get; set; }
        public string? Certificateur { get; set; }
        public DateTime? DateEnregistrement { get; set; }
        public DateTime? DateFinValidite { get; set; }
        public string? Description { get; set; }
        public List<string> Secteurs { get; set; } = new();
    }

    public class RncpCertificationInfo
    {
        public string RncpCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? Level { get; set; }
        public string? Certificateur { get; set; }
        public DateTime? DateEnregistrement { get; set; }
        public DateTime? DateFinValidite { get; set; }
        public string? FranceCompetencesUrl { get; set; }
        public string? Description { get; set; }
        public List<string> Secteurs { get; set; } = new();
    }

    public class RsCertificationInfo
    {
        public string RsCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Certificateur { get; set; }
        public DateTime? DateEnregistrement { get; set; }
        public DateTime? DateFinValidite { get; set; }
        public string? FranceCompetencesUrl { get; set; }
        public string? Description { get; set; }
    }
}
