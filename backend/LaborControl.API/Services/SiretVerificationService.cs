using System.Text.Json;

namespace LaborControl.API.Services
{
    public class SiretVerificationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SiretVerificationService> _logger;
        private readonly IConfiguration _configuration;

        public SiretVerificationService(HttpClient httpClient, ILogger<SiretVerificationService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<SiretValidationResult> VerifySiretAsync(string siret)
        {
            try
            {
                // Nettoyer le SIRET (enlever espaces)
                siret = siret.Replace(" ", "").Trim();

                // Validation format (14 chiffres)
                if (siret.Length != 14 || !siret.All(char.IsDigit))
                {
                    return new SiretValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Le SIRET doit contenir exactement 14 chiffres"
                    };
                }

                // Récupérer le token API INSEE depuis la configuration
                var inseeToken = _configuration["Insee:ApiToken"];

                if (string.IsNullOrEmpty(inseeToken))
                {
                    _logger.LogError("Token API INSEE non configuré");
                    return new SiretValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Service de vérification SIRET temporairement indisponible"
                    };
                }

                // Appel API INSEE officielle avec authentification
                var url = $"https://api.insee.fr/api-sirene/3.11/siret/{siret}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("X-INSEE-Api-Key-Integration", inseeToken);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("SIRET {Siret} non trouvé dans la base INSEE", siret);
                    return new SiretValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "SIRET non trouvé dans la base INSEE"
                    };
                }

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<InseeApiResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data?.Etablissement == null)
                {
                    return new SiretValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Données INSEE incomplètes"
                    };
                }

                var etablissement = data.Etablissement;

                var adresse = etablissement.AdresseEtablissement;

                // Vérifier l'état administratif depuis la période active (dateFin = null)
                var periodeActive = etablissement.PeriodesEtablissement?
                    .FirstOrDefault(p => p.DateFin == null);

                bool isActive = periodeActive?.EtatAdministratifEtablissement == "A";

                return new SiretValidationResult
                {
                    IsValid = true,
                    CompanyName = etablissement.UniteLegale?.DenominationUniteLegale
                                  ?? $"{etablissement.UniteLegale?.PrenomUsuelUniteLegale} {etablissement.UniteLegale?.NomUniteLegale}".Trim(),
                    Address = FormatStreetAddress(adresse),
                    PostalCode = adresse?.CodePostalEtablissement ?? "",
                    City = adresse?.LibelleCommuneEtablissement ?? "",
                    ActivityCode = etablissement.UniteLegale?.ActivitePrincipaleUniteLegale,
                    IsActive = isActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification SIRET {Siret}", siret);
                return new SiretValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Erreur lors de la vérification du SIRET"
                };
            }
        }

        private string FormatStreetAddress(AdresseEtablissement? adresse)
        {
            if (adresse == null) return string.Empty;

            var parts = new List<string>();

            if (!string.IsNullOrEmpty(adresse.NumeroVoieEtablissement))
                parts.Add(adresse.NumeroVoieEtablissement);

            if (!string.IsNullOrEmpty(adresse.TypeVoieEtablissement))
                parts.Add(adresse.TypeVoieEtablissement);

            if (!string.IsNullOrEmpty(adresse.LibelleVoieEtablissement))
                parts.Add(adresse.LibelleVoieEtablissement);

            return string.Join(" ", parts);
        }
    }

    public class SiretValidationResult
    {
        public bool IsValid { get; set; }
        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? PostalCode { get; set; }
        public string? City { get; set; }
        public string? ActivityCode { get; set; }
        public bool IsActive { get; set; }
        public string? ErrorMessage { get; set; }
    }

    // Classes pour désérialiser l'API INSEE
    public class InseeApiResponse
    {
        public Etablissement? Etablissement { get; set; }
    }

    public class Etablissement
    {
        public string? Siret { get; set; }
        public AdresseEtablissement? AdresseEtablissement { get; set; }
        public UniteLegale? UniteLegale { get; set; }
        public List<PeriodeEtablissement>? PeriodesEtablissement { get; set; }
    }

    public class PeriodeEtablissement
    {
        public string? DateFin { get; set; }
        public string? DateDebut { get; set; }
        public string? EtatAdministratifEtablissement { get; set; }
    }

    public class AdresseEtablissement
    {
        public string? NumeroVoieEtablissement { get; set; }
        public string? TypeVoieEtablissement { get; set; }
        public string? LibelleVoieEtablissement { get; set; }
        public string? CodePostalEtablissement { get; set; }
        public string? LibelleCommuneEtablissement { get; set; }
    }

    public class UniteLegale
    {
        public string? DenominationUniteLegale { get; set; }
        public string? PrenomUsuelUniteLegale { get; set; }
        public string? NomUniteLegale { get; set; }
        public string? ActivitePrincipaleUniteLegale { get; set; }
    }
}
