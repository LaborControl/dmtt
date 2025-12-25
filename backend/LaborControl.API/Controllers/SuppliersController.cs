using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.Models;
using LaborControl.API.DTOs;
using LaborControl.API.Services;
using System.Security.Claims;

namespace LaborControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SuppliersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SuppliersController> _logger;
        private readonly HttpClient _httpClient;
        private readonly SiretVerificationService _siretVerificationService;

        public SuppliersController(
            ApplicationDbContext context,
            ILogger<SuppliersController> logger,
            HttpClient httpClient,
            SiretVerificationService siretVerificationService)
        {
            _context = context;
            _logger = logger;
            _httpClient = httpClient;
            _siretVerificationService = siretVerificationService;
        }

        /// <summary>
        /// GET /api/suppliers
        /// Récupère tous les fournisseurs
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<SupplierResponse>>> GetSuppliers(
            [FromQuery] bool? isActive = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _context.Suppliers.AsQueryable();

                if (isActive.HasValue)
                {
                    query = query.Where(s => s.IsActive == isActive.Value);
                }

                var suppliers = await query
                    .OrderByDescending(s => s.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new SupplierResponse
                    {
                        Id = s.Id,
                        Name = s.Name,
                        ContactName = s.ContactName,
                        Email = s.Email,
                        Phone = s.Phone,
                        Address = s.Address,
                        City = s.City,
                        PostalCode = s.PostalCode,
                        Country = s.Country,
                        Siret = s.Siret,
                        Website = s.Website,
                        PaymentTerms = s.PaymentTerms,
                        LeadTimeDays = s.LeadTimeDays,
                        IsActive = s.IsActive,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt
                    })
                    .ToListAsync();

                _logger.LogInformation($"✅ {suppliers.Count} fournisseurs récupérés");
                return Ok(suppliers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur GetSuppliers");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/suppliers/{id}
        /// Récupère un fournisseur spécifique
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<SupplierResponse>> GetSupplier(Guid id)
        {
            try
            {
                var supplier = await _context.Suppliers
                    .Where(s => s.Id == id)
                    .Select(s => new SupplierResponse
                    {
                        Id = s.Id,
                        Name = s.Name,
                        ContactName = s.ContactName,
                        Email = s.Email,
                        Phone = s.Phone,
                        Address = s.Address,
                        City = s.City,
                        PostalCode = s.PostalCode,
                        Country = s.Country,
                        Siret = s.Siret,
                        Website = s.Website,
                        PaymentTerms = s.PaymentTerms,
                        LeadTimeDays = s.LeadTimeDays,
                        IsActive = s.IsActive,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (supplier == null)
                {
                    _logger.LogWarning($"⚠️ Fournisseur {id} non trouvé");
                    return NotFound(new { message = "Fournisseur non trouvé" });
                }

                return Ok(supplier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur GetSupplier {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/suppliers
        /// Crée un nouveau fournisseur avec validation conditionnelle (SIRET/TVA/TaxId)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<SupplierResponse>> CreateSupplier(CreateSupplierRequest request)
        {
            try
            {
                // Validation conditionnelle selon pays
                if (request.Country == "FR")
                {
                    // France → SIRET obligatoire
                    if (string.IsNullOrWhiteSpace(request.Siret) || request.Siret.Length != 14)
                    {
                        return BadRequest(new { message = "Le SIRET doit contenir 14 chiffres pour un fournisseur français" });
                    }

                    // Vérifier si le Siret existe déjà
                    var existingSiret = await _context.Suppliers
                        .FirstOrDefaultAsync(s => s.Siret == request.Siret);

                    if (existingSiret != null)
                    {
                        return Conflict(new { message = "Ce SIRET est déjà enregistré" });
                    }
                }
                else if (request.IsEuSupplier && request.Country != "FR")
                {
                    // UE (hors France) → TVA intracommunautaire obligatoire
                    if (string.IsNullOrWhiteSpace(request.VatNumber))
                    {
                        return BadRequest(new { message = "Le numéro TVA intracommunautaire est obligatoire pour un fournisseur UE" });
                    }

                    // Vérifier si le VatNumber existe déjà
                    var existingVat = await _context.Suppliers
                        .FirstOrDefaultAsync(s => s.VatNumber == request.VatNumber);

                    if (existingVat != null)
                    {
                        return Conflict(new { message = "Ce numéro TVA est déjà enregistré" });
                    }
                }

                var supplier = new Supplier
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    ContactName = request.ContactName,
                    Email = request.Email,
                    Phone = request.Phone,
                    Address = request.Address,
                    City = request.City,
                    PostalCode = request.PostalCode,
                    Country = request.Country,
                    Siret = request.Siret,
                    VatNumber = request.VatNumber,
                    TaxId = request.TaxId,
                    IsEuSupplier = request.IsEuSupplier,
                    Website = request.Website,
                    PaymentTerms = request.PaymentTerms,
                    LeadTimeDays = request.LeadTimeDays,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Fournisseur créé: {supplier.Name} (Pays: {supplier.Country}, IsEuSupplier: {supplier.IsEuSupplier})");

                return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, new SupplierResponse
                {
                    Id = supplier.Id,
                    Name = supplier.Name,
                    ContactName = supplier.ContactName,
                    Email = supplier.Email,
                    Phone = supplier.Phone,
                    Address = supplier.Address,
                    City = supplier.City,
                    PostalCode = supplier.PostalCode,
                    Country = supplier.Country,
                    Siret = supplier.Siret,
                    VatNumber = supplier.VatNumber,
                    TaxId = supplier.TaxId,
                    IsEuSupplier = supplier.IsEuSupplier,
                    Website = supplier.Website,
                    PaymentTerms = supplier.PaymentTerms,
                    LeadTimeDays = supplier.LeadTimeDays,
                    IsActive = supplier.IsActive,
                    CreatedAt = supplier.CreatedAt,
                    UpdatedAt = supplier.UpdatedAt
                });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "❌ Erreur BD CreateSupplier");
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                return StatusCode(500, new { message = "Erreur lors de la sauvegarde en base de données", details = innerMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur CreateSupplier");
                return StatusCode(500, new { message = "Erreur lors de la création du fournisseur", details = ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/suppliers/{id}
        /// Modifie un fournisseur
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSupplier(Guid id, UpdateSupplierRequest request)
        {
            try
            {
                var supplier = await _context.Suppliers.FindAsync(id);

                if (supplier == null)
                {
                    return NotFound(new { message = "Fournisseur non trouvé" });
                }

                supplier.Name = request.Name;
                supplier.ContactName = request.ContactName;
                supplier.Email = request.Email;
                supplier.Phone = request.Phone;
                supplier.Address = request.Address;
                supplier.City = request.City;
                supplier.PostalCode = request.PostalCode;
                supplier.Country = request.Country;
                supplier.Website = request.Website;
                supplier.PaymentTerms = request.PaymentTerms;
                supplier.LeadTimeDays = request.LeadTimeDays;
                supplier.IsActive = request.IsActive;
                supplier.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Fournisseur modifié: {supplier.Name}");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur UpdateSupplier {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// DELETE /api/suppliers/{id}
        /// Désactive un fournisseur
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupplier(Guid id)
        {
            try
            {
                var supplier = await _context.Suppliers.FindAsync(id);

                if (supplier == null)
                {
                    return NotFound(new { message = "Fournisseur non trouvé" });
                }

                supplier.IsActive = false;
                supplier.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Fournisseur désactivé: {supplier.Name}");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur DeleteSupplier {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/suppliers/validate/{siret}
        /// Valide un SIRET via l'API INSEE (endpoint pour Register page)
        /// </summary>
        [HttpGet("validate/{siret}")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> ValidateSiretGet(string siret)
        {
            try
            {
                // Nettoyer le SIRET
                siret = siret?.Replace(" ", "").Trim() ?? "";

                if (string.IsNullOrWhiteSpace(siret) || siret.Length != 14)
                {
                    return BadRequest(new { isValid = false, errorMessage = "SIRET invalide (14 chiffres requis)" });
                }

                // Utiliser le service de vérification qui inclut le token INSEE
                var result = await _siretVerificationService.VerifySiretAsync(siret);

                return Ok(new
                {
                    isValid = result.IsValid,
                    isActive = result.IsActive,
                    companyName = result.CompanyName,
                    address = result.Address,
                    postalCode = result.PostalCode,
                    city = result.City,
                    activityCode = result.ActivityCode,
                    activityLabel = result.ActivityCode, // Pour compatibilité avec le frontend
                    errorMessage = result.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur ValidateSiretGet");
                return StatusCode(500, new { isValid = false, errorMessage = "Erreur lors de la vérification du SIRET" });
            }
        }

        /// <summary>
        /// POST /api/suppliers/validate-siret
        /// Valide un SIRET via l'API INSEE
        /// </summary>
        [HttpPost("validate-siret")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> ValidateSiret([FromBody] string siret)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(siret) || siret.Length != 14)
                {
                    return BadRequest(new { isValid = false, message = "SIRET invalide (14 chiffres requis)" });
                }

                var result = await GetSiretDetails(siret);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur ValidateSiret");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Récupère les détails d'un SIRET via l'API INSEE
        /// </summary>
        private async Task<object> GetSiretDetails(string siret)
        {
            try
            {
                // Appel API INSEE (à adapter selon votre configuration)
                var response = await _httpClient.GetAsync($"https://api.insee.fr/entreprises/sirene/V3/siret/{siret}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
                    var root = jsonDoc.RootElement;

                    // Extraire les informations pertinentes
                    var etablissement = root.GetProperty("etablissement");
                    var uniteLegale = root.GetProperty("uniteLegale");

                    // Construire l'adresse en gérant les valeurs nulles
                    var numeroVoie = etablissement.TryGetProperty("numeroVoieEtablissement", out var nv) ? nv.GetString() : "";
                    var typeVoie = etablissement.TryGetProperty("typeVoieEtablissement", out var tv) ? tv.GetString() : "";
                    var libelleVoie = etablissement.TryGetProperty("libelleVoieEtablissement", out var lv) ? lv.GetString() : "";
                    var address = $"{numeroVoie} {typeVoie} {libelleVoie}".Trim();

                    return new
                    {
                        isValid = true,
                        isActive = etablissement.GetProperty("etatAdministratifEtablissement").GetString() == "A",
                        companyName = uniteLegale.GetProperty("denominationUniteLegale").GetString(),
                        address = address,
                        postalCode = etablissement.TryGetProperty("codePostalEtablissement", out var cp) ? cp.GetString() : "",
                        city = etablissement.TryGetProperty("libelleCommuneEtablissement", out var lc) ? lc.GetString() : "",
                        activityCode = etablissement.TryGetProperty("codeActivitePrincipaleEtablissement", out var ac) ? ac.GetString() : "",
                        activityLabel = "Activité principale"
                    };
                }
                else
                {
                    _logger.LogWarning($"⚠️ SIRET {siret} non trouvé (HTTP {response.StatusCode})");
                    return new
                    {
                        isValid = false,
                        isActive = false,
                        errorMessage = "SIRET non trouvé dans la base INSEE"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur GetSiretDetails pour SIRET {siret}");
                return new
                {
                    isValid = false,
                    isActive = false,
                    errorMessage = "Erreur lors de la vérification du SIRET"
                };
            }
        }
    }
}
