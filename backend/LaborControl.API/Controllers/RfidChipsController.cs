using System.Security.Claims;
using System.Text;
using LaborControl.API.Data;
using LaborControl.API.DTOs;
using LaborControl.API.Models;
using LaborControl.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace LaborControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RfidChipsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRfidSecurityService _rfidSecurityService;
        private readonly ILogger<RfidChipsController> _logger;

        public RfidChipsController(
            ApplicationDbContext context,
            IRfidSecurityService rfidSecurityService,
            ILogger<RfidChipsController> logger)
        {
            _context = context;
            _rfidSecurityService = rfidSecurityService;
            _logger = logger;
        }

        /// <summary>
        /// Récupère l'ID du client depuis le JWT
        /// </summary>
        private Guid GetCustomerId()
        {
            var customerId = User.FindFirst("CustomerId")?.Value;
            if (string.IsNullOrEmpty(customerId))
            {
                _logger.LogError("❌ CustomerId non trouvé dans le token JWT");
                throw new UnauthorizedAccessException("CustomerId non trouvé dans le token");
            }
            if (!Guid.TryParse(customerId, out var parsedCustomerId))
            {
                _logger.LogError($"❌ CustomerId invalide: {customerId}");
                throw new UnauthorizedAccessException("CustomerId invalide");
            }
            return parsedCustomerId;
        }

        /// <summary>
        /// Récupère l'ID de l'utilisateur depuis le JWT
        /// </summary>
        private Guid GetUserId()
        {
            // Essayer d'abord "sub", puis "NameIdentifier"
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("❌ UserId non trouvé dans le token JWT");
                throw new UnauthorizedAccessException("UserId non trouvé dans le token");
            }
            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                _logger.LogError($"❌ UserId invalide: {userId}");
                throw new UnauthorizedAccessException("UserId invalide");
            }
            return parsedUserId;
        }

        /// <summary>
        /// Enregistre un changement de statut dans l'historique
        /// </summary>
        private async System.Threading.Tasks.Task RecordStatusChange(RfidChip chip, string oldStatus, string newStatus, string? notes = null)
        {
            try
            {
                var userId = GetUserId();
                var history = new RfidChipStatusHistory
                {
                    Id = Guid.NewGuid(),
                    RfidChipId = chip.Id,
                    FromStatus = oldStatus,
                    ToStatus = newStatus,
                    ChangedAt = DateTime.UtcNow,
                    ChangedBy = userId,
                    Notes = notes
                };

                _context.RfidChipStatusHistory.Add(history);
                _logger.LogInformation($"📝 Changement de statut enregistré: {chip.ChipId} ({oldStatus} → {newStatus})");
                await System.Threading.Tasks.Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de l'enregistrement du changement de statut pour {chip.ChipId}");
            }
        }

        /// <summary>
        /// Valide que l'abonnement du client est actif
        /// </summary>
        private async Task<bool> ValidateSubscription(Guid customerId)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(customerId);
                if (customer == null)
                {
                    _logger.LogError($"❌ Client {customerId} non trouvé");
                    return false;
                }

                // Vérifier si l'abonnement est actif
                // TODO: Ajouter logique vérification abonnement selon votre modèle Customer
                _logger.LogInformation($"✅ Abonnement validé pour client {customerId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur validation abonnement pour {customerId}");
                return false;
            }
        }

        /// <summary>
        /// Génère une commande de garantie à 0€ pour SAV
        /// </summary>
        private async Task<Order?> GenerateWarrantyOrder(RfidChip chip, string reason)
        {
            try
            {
                // Les commandes de garantie nécessitent un CustomerId
                if (!chip.CustomerId.HasValue)
                {
                    _logger.LogError($"❌ Impossible de créer une commande garantie pour la puce {chip.ChipId} - Aucun CustomerId");
                    return null;
                }

                var warrantyOrder = new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerId = chip.CustomerId.Value,
                    OrderNumber = $"WARRANTY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                    TotalAmount = 0,
                    Status = "PENDING",
                    DeliveryAddress = "SAV",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(warrantyOrder);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Commande garantie créée: {warrantyOrder.OrderNumber} pour puce {chip.ChipId} (Raison: {reason})");
                return warrantyOrder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur génération commande garantie pour {chip.ChipId}");
                return null;
            }
        }

        /// <summary>
        /// Transition une puce vers un nouvel état avec enregistrement historique
        /// </summary>
        private async Task<bool> TransitionToState(RfidChip chip, string newStatus, string? notes = null)
        {
            try
            {
                var oldStatus = chip.Status;
                chip.Status = newStatus;
                chip.UpdatedAt = DateTime.UtcNow;

                await RecordStatusChange(chip, oldStatus, newStatus, notes);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Transition réussie: {chip.ChipId} ({oldStatus} → {newStatus})");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur transition pour {chip.ChipId}");
                return false;
            }
        }

        /// <summary>
        /// GET /api/rfidchips
        /// Récupère toutes les puces du client
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RfidChipResponse>>> GetChips(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var customerId = GetCustomerId();

                var query = _context.RfidChips
                    .Where(c => c.CustomerId == customerId);

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(c => c.Status == status);
                }

                var chips = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new RfidChipResponse
                    {
                        Id = c.Id,
                        ChipId = c.ChipId,
                        Uid = c.Uid,
                        CustomerId = c.CustomerId,
                        Status = c.Status,
                        ActivationDate = c.ActivationDate,
                        CreatedAt = c.CreatedAt,
                        PackagingCode = c.PackagingCode
                    })
                    .ToListAsync();

                _logger.LogInformation($"✅ {chips.Count} puces récupérées pour client {customerId}");
                return Ok(chips);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur GetChips");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/rfidchips/{id}
        /// Récupère une puce spécifique
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<RfidChipResponse>> GetChip(Guid id)
        {
            try
            {
                var customerId = GetCustomerId();

                var chip = await _context.RfidChips
                    .Where(c => c.Id == id && c.CustomerId == customerId)
                    .Select(c => new RfidChipResponse
                    {
                        Id = c.Id,
                        ChipId = c.ChipId,
                        Uid = c.Uid,
                        CustomerId = c.CustomerId,
                        Status = c.Status,
                        ActivationDate = c.ActivationDate,
                        CreatedAt = c.CreatedAt,
                        PackagingCode = c.PackagingCode
                    })
                    .FirstOrDefaultAsync();

                if (chip == null)
                {
                    _logger.LogWarning($"⚠️ Puce {id} non trouvée pour client {customerId}");
                    return NotFound(new { message = "Puce non trouvée" });
                }

                return Ok(chip);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur GetChip {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/rfidchips/register-single
        /// Enregistre une puce unique (ACR122U ou scan manuel)
        /// </summary>
        [HttpPost("register-single")]
        public async Task<ActionResult<RfidChipResponse>> RegisterSingleChip(RegisterSingleChipRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Uid))
                {
                    return BadRequest(new { message = "L'UID est obligatoire" });
                }

                var customerId = GetCustomerId();
                var userId = GetUserId();

                // Vérifier si la puce existe déjà
                var existingChip = await _context.RfidChips
                    .FirstOrDefaultAsync(c => c.Uid == request.Uid.Trim());

                if (existingChip != null)
                {
                    _logger.LogWarning($"⚠️ Tentative d'enregistrement d'une puce déjà existante: {request.Uid}");
                    return Conflict(new
                    {
                        message = "Cette puce est déjà enregistrée",
                        chip = new RfidChipResponse
                        {
                            Id = existingChip.Id,
                            ChipId = existingChip.ChipId,
                            Uid = existingChip.Uid,
                            Status = existingChip.Status
                        }
                    });
                }

                // Générer Salt, ChipId et Checksum
                var salt = _rfidSecurityService.GenerateSalt();
                var chipId = _rfidSecurityService.GenerateChipId();
                var checksum = _rfidSecurityService.GenerateChecksum(request.Uid, salt, chipId);

                // Créer la puce
                var chip = new RfidChip
                {
                    Id = Guid.NewGuid(),
                    ChipId = chipId,
                    Uid = request.Uid.Trim(),
                    CustomerId = customerId,
                    OrderId = request.OrderId,
                    Salt = salt,
                    Checksum = checksum,
                    Status = "EN_TRANSIT",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = userId.ToString()
                };

                _context.RfidChips.Add(chip);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Puce enregistrée: {chipId} (UID: {request.Uid})");

                return CreatedAtAction(nameof(GetChip), new { id = chip.Id }, new RfidChipResponse
                {
                    Id = chip.Id,
                    ChipId = chip.ChipId,
                    Uid = chip.Uid,
                    CustomerId = chip.CustomerId,
                    Status = chip.Status,
                    ActivationDate = chip.ActivationDate,
                    CreatedAt = chip.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur RegisterSingleChip");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/rfidchips/import-excel
        /// Importe des puces depuis un fichier Excel
        /// ⚠️ VALIDATION : Vérifie que la quantité d'UIDs correspond à la commande
        /// Staff: Importe des puces en stock (CustomerId = null)
        /// Client: Importe des puces pour le client (CustomerId depuis token)
        /// </summary>
        [HttpPost("import-excel")]
        public async Task<ActionResult<ImportChipsResponse>> ImportFromExcel(ImportChipsRequest request)
        {
            try
            {
                if (request.Uids == null || request.Uids.Count == 0)
                {
                    return BadRequest(new { message = "Aucun UID fourni" });
                }

                // Vérifier si l'utilisateur est STAFF ou CLIENT
                var userType = User.FindFirst("UserType")?.Value;
                var isStaff = userType == "STAFF";

                Guid? customerId = null;
                if (!isStaff)
                {
                    // Pour les clients, récupérer le CustomerId depuis le token
                    customerId = GetCustomerId();
                }
                // Pour le staff, customerId reste null (puces en stock)

                var userId = GetUserId();

                // ⚠️ VALIDATION : Vérifier que la commande existe et récupérer la quantité attendue
                var order = await _context.SupplierOrders
                    .Include(o => o.Lines)
                    .FirstOrDefaultAsync(o => o.Id == request.OrderId);

                if (order == null)
                {
                    return BadRequest(new { message = "Commande fournisseur non trouvée" });
                }

                // Calculer la quantité totale commandée
                var expectedQuantity = order.Lines.Sum(l => l.Quantity);

                // Comparer avec le nombre d'UIDs fournis
                if (request.Uids.Count != expectedQuantity)
                {
                    _logger.LogWarning($"⚠️ Anomalie détectée lors de l'import: {order.OrderNumber} - Attendu: {expectedQuantity}, Fourni: {request.Uids.Count}");
                    return BadRequest(new
                    {
                        message = "Anomalie détectée - Import bloqué",
                        details = $"La quantité d'UIDs ne correspond pas à la commande",
                        orderNumber = order.OrderNumber,
                        expectedQuantity = expectedQuantity,
                        providedQuantity = request.Uids.Count,
                        difference = request.Uids.Count - expectedQuantity,
                        action = "Vérifiez le fichier Excel avec le service achat"
                    });
                }

                var response = new ImportChipsResponse();

                foreach (var uid in request.Uids)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(uid))
                        {
                            response.ErrorCount++;
                            response.Errors.Add("UID vide");
                            continue;
                        }

                        var trimmedUid = uid.Trim();

                        // Vérifier si la puce existe déjà
                        var existingChip = await _context.RfidChips
                            .FirstOrDefaultAsync(c => c.Uid == trimmedUid);

                        if (existingChip != null)
                        {
                            response.DuplicateCount++;
                            response.Errors.Add($"Doublon: {trimmedUid}");
                            continue;
                        }

                        // Générer uniquement le ChipId (Salt et Checksum seront générés lors de l'encodage physique)
                        var chipId = _rfidSecurityService.GenerateChipId();

                        // Créer la puce
                        var chip = new RfidChip
                        {
                            Id = Guid.NewGuid(),
                            ChipId = chipId,
                            Uid = trimmedUid,
                            CustomerId = customerId,
                            SupplierOrderId = request.OrderId,
                            Salt = null,  // Sera généré lors de l'encodage physique
                            Checksum = null,  // Sera généré lors de l'encodage physique
                            Status = "EN_TRANSIT",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            CreatedBy = userId.ToString()
                        };

                        _context.RfidChips.Add(chip);
                        response.SuccessCount++;
                        response.CreatedChips.Add(new RfidChipResponse
                        {
                            Id = chip.Id,
                            ChipId = chip.ChipId,
                            Uid = chip.Uid,
                            Status = chip.Status
                        });
                    }
                    catch (Exception ex)
                    {
                        response.ErrorCount++;
                        response.Errors.Add($"Erreur {uid}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Import Excel: {response.SuccessCount} succès, {response.DuplicateCount} doublons, {response.ErrorCount} erreurs");

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur ImportFromExcel");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/rfidchips/parse-excel
        /// Parse un fichier Excel et extrait les UIDs
        /// </summary>
        [HttpPost("parse-excel")]
        public async Task<ActionResult<ParseExcelResponse>> ParseExcel(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "Aucun fichier fourni" });
                }

                // Vérifier l'extension du fichier
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension != ".xlsx" && extension != ".xls")
                {
                    return BadRequest(new { message = "Format de fichier non supporté. Utilisez .xlsx ou .xls" });
                }

                var uids = new List<string>();

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);

                    // Utiliser EPPlus pour lire le fichier
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];

                        // Trouver la colonne "UID"
                        int uidColumnIndex = -1;
                        for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                        {
                            var headerValue = worksheet.Cells[1, col].Text?.Trim().ToUpper();
                            if (headerValue == "UID")
                            {
                                uidColumnIndex = col;
                                break;
                            }
                        }

                        if (uidColumnIndex == -1)
                        {
                            return BadRequest(new { message = "Colonne 'UID' non trouvée dans le fichier" });
                        }

                        // Extraire les UIDs (en ignorant la ligne d'en-tête)
                        for (int row = 2; row <= worksheet.Dimension.Rows; row++)
                        {
                            var uid = worksheet.Cells[row, uidColumnIndex].Text?.Trim();
                            if (!string.IsNullOrEmpty(uid) && uid.Length >= 14)
                            {
                                uids.Add(uid);
                            }
                        }
                    }
                }

                _logger.LogInformation($"✅ Parsing Excel : {uids.Count} UIDs extraits");

                return Ok(new ParseExcelResponse
                {
                    Uids = uids,
                    TotalRows = uids.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur ParseExcel");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/rfidchips/validate-scan
        /// Valide un scan NFC (sécurité multi-couche)
        /// </summary>
        [HttpPost("validate-scan")]
        public async Task<ActionResult<ValidateScanResponse>> ValidateScan(ValidateScanRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Uid))
                {
                    return BadRequest(new { message = "L'UID est obligatoire" });
                }

                var customerId = GetCustomerId();
                var trimmedUid = request.Uid.Trim();

                // 1. Vérifier que l'UID existe en BD (whitelist stricte)
                var chip = await _context.RfidChips
                    .FirstOrDefaultAsync(c => c.Uid == trimmedUid && c.CustomerId == customerId);

                if (chip == null)
                {
                    _logger.LogWarning($"🚫 Tentative de scan d'une puce inconnue: {trimmedUid} (client: {customerId})");
                    return Unauthorized(new ValidateScanResponse
                    {
                        IsValid = false,
                        Message = "Puce non autorisée"
                    });
                }

                // 2. Vérifier le statut
                if (chip.Status != "ACTIVE")
                {
                    _logger.LogWarning($"⚠️ Tentative de scan d'une puce inactive: {trimmedUid} (status: {chip.Status})");
                    return BadRequest(new ValidateScanResponse
                    {
                        IsValid = false,
                        Message = $"Puce inactive (statut: {chip.Status})"
                    });
                }

                // 3. Vérifier le checksum HMAC (sécurité supplémentaire)
                var isChecksumValid = _rfidSecurityService.ValidateChecksum(trimmedUid, chip.Salt, chip.ChipId, chip.Checksum);
                if (!isChecksumValid)
                {
                    _logger.LogError($"🚫 ALERTE SÉCURITÉ: Checksum invalide pour {trimmedUid} - Tentative de clonage?");
                    return Unauthorized(new ValidateScanResponse
                    {
                        IsValid = false,
                        Message = "Checksum invalide - Tentative de clonage détectée"
                    });
                }

                _logger.LogInformation($"✅ Scan valide: {chip.ChipId} (UID: {trimmedUid})");

                return Ok(new ValidateScanResponse
                {
                    IsValid = true,
                    ChipId = chip.ChipId,
                    Message = "Puce valide",
                    ControlPointId = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur ValidateScan");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/rfidchips/check-exists/{uid}
        /// Vérifie si une puce existe déjà
        /// </summary>
        [HttpGet("check-exists/{uid}")]
        public async Task<ActionResult<CheckChipExistsResponse>> CheckChipExists(string uid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(uid))
                {
                    return BadRequest(new { message = "L'UID est obligatoire" });
                }

                var customerId = GetCustomerId();
                var trimmedUid = uid.Trim();

                var chip = await _context.RfidChips
                    .Where(c => c.Uid == trimmedUid && c.CustomerId == customerId)
                    .Select(c => new RfidChipResponse
                    {
                        Id = c.Id,
                        ChipId = c.ChipId,
                        Uid = c.Uid,
                        Status = c.Status,
                        CreatedAt = c.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                return Ok(new CheckChipExistsResponse
                {
                    Exists = chip != null,
                    Chip = chip
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur CheckChipExists");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/rfidchips/{id}/status-history
        /// Récupère l'historique des changements de statut d'une puce
        /// </summary>
        [HttpGet("{id}/status-history")]
        public async Task<ActionResult<IEnumerable<object>>> GetStatusHistory(Guid id)
        {
            try
            {
                var customerId = GetCustomerId();

                // Vérifier que la puce appartient au client
                var chip = await _context.RfidChips
                    .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == customerId);

                if (chip == null)
                {
                    return NotFound(new { message = "Puce non trouvée" });
                }

                // Récupérer l'historique
                var history = await _context.RfidChipStatusHistory
                    .Where(h => h.RfidChipId == id)
                    .OrderByDescending(h => h.ChangedAt)
                    .Select(h => new
                    {
                        h.Id,
                        h.FromStatus,
                        h.ToStatus,
                        h.ChangedAt,
                        h.Notes,
                        ChangedByUserId = h.ChangedBy
                    })
                    .ToListAsync();

                _logger.LogInformation($"✅ Historique récupéré pour puce {chip.ChipId}: {history.Count} entrées");

                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur GetStatusHistory {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/rfidchips/{id}/assign
        /// Assigne une puce à un client
        /// </summary>
        [HttpPut("{id}/assign")]
        public async Task<IActionResult> AssignChip(Guid id, AssignChipsRequest request)
        {
            try
            {
                var chip = await _context.RfidChips.FindAsync(id);

                if (chip == null)
                {
                    return NotFound(new { message = "Puce non trouvée" });
                }

                var oldStatus = chip.Status;
                chip.CustomerId = request.CustomerId;
                chip.OrderId = request.OrderId;
                chip.Status = "IN_TRANSIT";
                chip.ShippedToClientDate = DateTime.UtcNow;
                chip.PackagingCode = $"PKG-{DateTime.UtcNow:yyyy-MM-dd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
                chip.UpdatedAt = DateTime.UtcNow;

                // Enregistrer le changement de statut
                await RecordStatusChange(chip, oldStatus, "IN_TRANSIT", $"Assignée au client {request.CustomerId}");

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Puce {chip.ChipId} assignée au client {request.CustomerId}");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur AssignChip {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// DELETE /api/rfidchips/{id}
        /// Désactive une puce
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChip(Guid id)
        {
            try
            {
                var customerId = GetCustomerId();

                var chip = await _context.RfidChips
                    .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == customerId);

                if (chip == null)
                {
                    return NotFound(new { message = "Puce non trouvée" });
                }

                var oldStatus = chip.Status;
                chip.Status = "INACTIVE";
                chip.DeactivationDate = DateTime.UtcNow;
                chip.UpdatedAt = DateTime.UtcNow;

                // Enregistrer le changement de statut
                await RecordStatusChange(chip, oldStatus, "INACTIVE", "Puce désactivée");

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Puce {chip.ChipId} désactivée");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur DeleteChip {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/rfidchips/{id}/archive
        /// Archive une puce avec motif (État ARCHIVEE)
        /// </summary>
        [HttpPut("{id}/archive")]
        public async Task<IActionResult> ArchiveChip(Guid id, [FromBody] ArchiveChipRequest request)
        {
            try
            {
                var customerId = GetCustomerId();

                var chip = await _context.RfidChips
                    .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == customerId);

                if (chip == null)
                {
                    return NotFound(new { message = "Puce non trouvée" });
                }

                // Valider le motif (minimum 10 mots)
                if (string.IsNullOrWhiteSpace(request.Reason))
                {
                    return BadRequest(new { message = "Le motif d'archivage est obligatoire" });
                }

                var wordCount = request.Reason.Trim().Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
                if (wordCount < 10)
                {
                    return BadRequest(new { message = $"Le motif doit contenir au minimum 10 mots (actuellement: {wordCount})" });
                }

                if (chip.Status == "ARCHIVEE")
                {
                    return BadRequest(new { message = "Cette puce est déjà archivée" });
                }

                var oldStatus = chip.Status;
                chip.Status = "ARCHIVEE";
                chip.UpdatedAt = DateTime.UtcNow;

                // Enregistrer le changement de statut avec le motif
                await RecordStatusChange(chip, oldStatus, "ARCHIVEE", $"Archivée - Motif: {request.Reason}");

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Puce {chip.ChipId} archivée - Motif: {request.Reason}");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur ArchiveChip {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ============================================
        // PHASE 4 : NOUVEAUX ENDPOINTS (10 endpoints)
        // ============================================

        /// <summary>
        /// PUT /api/rfidchips/{id}/receive-from-supplier
        /// État 2 : EN_ATELIER - Réception du fournisseur
        /// </summary>
        [HttpPut("{id}/receive-from-supplier")]
        public async Task<IActionResult> ReceiveFromSupplier(Guid id, [FromBody] ReceiveFromSupplierRequest request)
        {
            try
            {
                var customerId = GetCustomerId();
                var chip = await _context.RfidChips
                    .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == customerId);

                if (chip == null)
                    return NotFound(new { message = "Puce non trouvée" });

                if (chip.Status != "EN_TRANSIT")
                    return BadRequest(new { message = $"Transition invalide: {chip.Status} → EN_ATELIER" });

                chip.ReceivedFromSupplierDate = DateTime.UtcNow;
                await TransitionToState(chip, "EN_ATELIER", $"Reçue du fournisseur - Commande: {request.SupplierOrderId}");

                _logger.LogInformation($"✅ Puce {chip.ChipId} reçue du fournisseur");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur ReceiveFromSupplier {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/rfidchips/{id}/encode
        /// État 3 : EN_STOCK - Cryptage et préparation
        /// </summary>
        [HttpPut("{id}/encode")]
        public async Task<IActionResult> EncodeChip(Guid id)
        {
            try
            {
                var customerId = GetCustomerId();
                var chip = await _context.RfidChips
                    .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == customerId);

                if (chip == null)
                    return NotFound(new { message = "Puce non trouvée" });

                if (chip.Status != "EN_ATELIER")
                    return BadRequest(new { message = $"Transition invalide: {chip.Status} → EN_STOCK" });

                chip.EncodingDate = DateTime.UtcNow;
                await TransitionToState(chip, "EN_STOCK", "Puce cryptée et prête pour commande");

                _logger.LogInformation($"✅ Puce {chip.ChipId} cryptée (Salt: {chip.Salt.Substring(0, 8)}...)");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur EncodeChip {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/rfidchips/{id}/ship-to-client
        /// État 4 : EN_LIVRAISON - Expédition au client
        /// </summary>
        [HttpPut("{id}/ship-to-client")]
        public async Task<IActionResult> ShipToClient(Guid id, [FromBody] ShipToClientRequest request)
        {
            try
            {
                var customerId = GetCustomerId();
                var chip = await _context.RfidChips
                    .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == customerId);

                if (chip == null)
                    return NotFound(new { message = "Puce non trouvée" });

                if (chip.Status != "EN_STOCK")
                    return BadRequest(new { message = $"Transition invalide: {chip.Status} → EN_LIVRAISON" });

                chip.ClientOrderId = request.ClientOrderId;
                chip.ShippedToClientDate = DateTime.UtcNow;
                chip.PackagingCode = $"PKG-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

                await TransitionToState(chip, "EN_LIVRAISON", $"Expédiée au client - PKG: {chip.PackagingCode}");

                _logger.LogInformation($"✅ Puce {chip.ChipId} expédiée (PKG: {chip.PackagingCode})");
                return Ok(new { packagingCode = chip.PackagingCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur ShipToClient {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/rfidchips/{id}/confirm-delivery
        /// État 5 : LIVREE - Confirmation réception client
        /// </summary>
        [HttpPut("{id}/confirm-delivery")]
        public async Task<IActionResult> ConfirmDelivery(Guid id, [FromBody] ConfirmDeliveryRequest request)
        {
            try
            {
                var customerId = GetCustomerId();
                var chip = await _context.RfidChips
                    .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == customerId);

                if (chip == null)
                    return NotFound(new { message = "Puce non trouvée" });

                if (chip.Status != "EN_LIVRAISON")
                    return BadRequest(new { message = $"Transition invalide: {chip.Status} → LIVREE" });

                // Vérifier l'abonnement
                if (!await ValidateSubscription(customerId))
                    return BadRequest(new { message = "Abonnement inactif - Livraison refusée" });

                chip.DeliveredToClientDate = DateTime.UtcNow;
                await TransitionToState(chip, "LIVREE", $"Livrée au client - PKG confirmé: {request.PackagingCode}");

                _logger.LogInformation($"✅ Puce {chip.ChipId} livrée et confirmée");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur ConfirmDelivery {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/rfidchips/{id}/assign-to-controlpoint
        /// État 7 : ACTIVE - Assignation à point de contrôle
        /// </summary>
        [HttpPut("{id}/assign-to-controlpoint")]
        public async Task<IActionResult> AssignToControlPoint(Guid id, [FromBody] AssignToControlPointRequest request)
        {
            try
            {
                var customerId = GetCustomerId();
                var chip = await _context.RfidChips
                    .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == customerId);

                if (chip == null)
                    return NotFound(new { message = "Puce non trouvée" });

                // Vérifier que le point de contrôle existe
                var controlPoint = await _context.ControlPoints
                    .FirstOrDefaultAsync(cp => cp.Id == request.ControlPointId && cp.CustomerId == customerId);

                if (controlPoint == null)
                    return BadRequest(new { message = "Point de contrôle non trouvé" });

                // Transition depuis LIVREE ou INACTIVE
                if (chip.Status != "LIVREE" && chip.Status != "INACTIVE")
                    return BadRequest(new { message = $"Transition invalide: {chip.Status} → ACTIVE" });

                chip.ControlPointId = request.ControlPointId;
                chip.AssignmentDate = DateTime.UtcNow;
                chip.FirstScanDate = DateTime.UtcNow; // Scan de validation
                await TransitionToState(chip, "ACTIVE", $"Assignée au point de contrôle {controlPoint.Name}");

                _logger.LogInformation($"✅ Puce {chip.ChipId} assignée au point {controlPoint.Name}");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur AssignToControlPoint {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/rfidchips/{id}/request-sav
        /// État 8 : RETOUR_SAV - Demande de SAV
        /// </summary>
        [HttpPost("{id}/request-sav")]
        public async Task<IActionResult> RequestSav(Guid id, [FromBody] RequestSavRequest request)
        {
            try
            {
                var customerId = GetCustomerId();
                var chip = await _context.RfidChips
                    .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == customerId);

                if (chip == null)
                    return NotFound(new { message = "Puce non trouvée" });

                if (chip.Status != "ACTIVE")
                    return BadRequest(new { message = $"Transition invalide: {chip.Status} → RETOUR_SAV" });

                chip.SavReason = request.Reason;
                chip.SavReturnDate = DateTime.UtcNow;

                // Générer commande garantie 0€
                var warrantyOrder = await GenerateWarrantyOrder(chip, request.Reason);
                if (warrantyOrder == null)
                    return StatusCode(500, new { message = "Erreur création commande garantie" });

                await TransitionToState(chip, "RETOUR_SAV", $"SAV demandée - Raison: {request.Reason} - Commande garantie: {warrantyOrder.OrderNumber}");

                _logger.LogInformation($"✅ Puce {chip.ChipId} en SAV - Commande garantie: {warrantyOrder.OrderNumber}");
                return Ok(new { warrantyOrderId = warrantyOrder.Id, orderNumber = warrantyOrder.OrderNumber });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur RequestSav {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/rfidchips/{id}/receive-sav
        /// État 9 : RECEPTION_SAV - Réception SAV
        /// </summary>
        [HttpPut("{id}/receive-sav")]
        public async Task<IActionResult> ReceiveSav(Guid id)
        {
            try
            {
                var customerId = GetCustomerId();
                var chip = await _context.RfidChips
                    .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == customerId);

                if (chip == null)
                    return NotFound(new { message = "Puce non trouvée" });

                if (chip.Status != "RETOUR_SAV")
                    return BadRequest(new { message = $"Transition invalide: {chip.Status} → RECEPTION_SAV" });

                await TransitionToState(chip, "RECEPTION_SAV", "Reçue par SAV - Nouveau cycle autorisé");

                _logger.LogInformation($"✅ Puce {chip.ChipId} reçue par SAV");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur ReceiveSav {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/rfidchips/{id}/replace
        /// État 10 : REMPLACEE - Remplacement de puce
        /// </summary>
        [HttpPut("{id}/replace")]
        public async Task<IActionResult> ReplaceChip(Guid id, [FromBody] ReplaceChipRequest request)
        {
            try
            {
                var customerId = GetCustomerId();
                var oldChip = await _context.RfidChips
                    .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == customerId);

                if (oldChip == null)
                    return NotFound(new { message = "Puce non trouvée" });

                if (oldChip.Status != "RETOUR_SAV" && oldChip.Status != "RECEPTION_SAV")
                    return BadRequest(new { message = $"Transition invalide: {oldChip.Status} → REMPLACEE" });

                // Vérifier que la puce de remplacement existe
                var newChip = await _context.RfidChips
                    .FirstOrDefaultAsync(c => c.Id == request.ReplacementChipId && c.CustomerId == customerId);

                if (newChip == null)
                    return BadRequest(new { message = "Puce de remplacement non trouvée" });

                oldChip.ReplacementChipId = request.ReplacementChipId;
                await TransitionToState(oldChip, "REMPLACEE", $"Remplacée par puce {newChip.ChipId}");

                // Archiver automatiquement si la puce de remplacement est LIVREE
                if (newChip.Status == "LIVREE")
                {
                    oldChip.Status = "ARCHIVEE";
                    await RecordStatusChange(oldChip, "REMPLACEE", "ARCHIVEE", "Archivée automatiquement");
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"✅ Puce {oldChip.ChipId} archivée automatiquement");
                }

                _logger.LogInformation($"✅ Puce {oldChip.ChipId} remplacée par {newChip.ChipId}");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur ReplaceChip {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/rfidchips/count-by-order/{orderId}
        /// Compte le nombre de puces importées pour une commande fournisseur
        /// </summary>
        [HttpGet("count-by-order/{orderId}")]
        public async Task<ActionResult<int>> CountBySupplierOrder(Guid orderId)
        {
            try
            {
                var customerId = GetCustomerId();

                var count = await _context.RfidChips
                    .Where(c => c.OrderId == orderId && c.CustomerId == customerId)
                    .CountAsync();

                _logger.LogInformation($"✅ Nombre de puces pour commande {orderId}: {count}");
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur CountBySupplierOrder {orderId}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/rfidchips/stats/by-status
        /// Statistiques des puces par état
        /// </summary>
        [HttpGet("stats/by-status")]
        public async Task<ActionResult<object>> GetStatsByStatus()
        {
            try
            {
                var customerId = GetCustomerId();

                var stats = await _context.RfidChips
                    .Where(c => c.CustomerId == customerId)
                    .GroupBy(c => c.Status)
                    .Select(g => new
                    {
                        status = g.Key,
                        count = g.Count(),
                        percentage = (g.Count() * 100.0) / _context.RfidChips.Where(c => c.CustomerId == customerId).Count()
                    })
                    .OrderByDescending(s => s.count)
                    .ToListAsync();

                var total = stats.Sum(s => s.count);

                _logger.LogInformation($"✅ Statistiques récupérées: {total} puces");
                return Ok(new { total, byStatus = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur GetStatsByStatus");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/rfidchips/export/csv
        /// Export CSV de toutes les puces
        /// </summary>
        [HttpGet("export/csv")]
        public async Task<IActionResult> ExportCsv()
        {
            try
            {
                var customerId = GetCustomerId();

                var chips = await _context.RfidChips
                    .Where(c => c.CustomerId == customerId)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                var csv = "ChipId,UID,Status,CreatedAt,PackagingCode,ControlPointId,SavReason\n";
                foreach (var chip in chips)
                {
                    csv += $"\"{chip.ChipId}\",\"{chip.Uid}\",\"{chip.Status}\",\"{chip.CreatedAt:yyyy-MM-dd HH:mm:ss}\",\"{chip.PackagingCode ?? ""}\",\"{chip.ControlPointId ?? Guid.Empty}\",\"{chip.SavReason ?? ""}\"\n";
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                _logger.LogInformation($"✅ Export CSV: {chips.Count} puces");
                return File(bytes, "text/csv", $"rfidchips-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur ExportCsv");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Client scanne une puce pour la première fois - Assignation automatique à la commande la plus ancienne
        /// Workflow: EN_STOCK → INACTIVE (1er scan client)
        /// </summary>
        [HttpPost("activate-chip")]
        [Authorize]
        public async Task<ActionResult> ActivateChip([FromBody] ActivateChipRequest request)
        {
            try
            {
                // Récupérer le CustomerId depuis le token
                var customerIdClaim = User.Claims.FirstOrDefault(c => c.Type == "CustomerId");
                if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
                    return Unauthorized(new { message = "Token invalide" });

                _logger.LogInformation("[ACTIVATE-CHIP] Client {CustomerId} scanne UID: {Uid}", customerId, request.Uid);

                // 1. Récupérer la puce
                var chip = await _context.RfidChips.FirstOrDefaultAsync(c => c.Uid == request.Uid);
                if (chip == null)
                    return NotFound(new { message = "Puce inconnue" });

                // 2. Vérifier que la puce est EN_STOCK
                if (chip.Status != "EN_STOCK")
                {
                    _logger.LogWarning("[ACTIVATE-CHIP] Puce {Uid} n'est pas EN_STOCK (status actuel: {Status})", request.Uid, chip.Status);
                    return BadRequest(new { message = $"Cette puce ne peut pas être activée (statut: {chip.Status})" });
                }

                // 2bis. VÉRIFICATION AUTHENTICITÉ ANTI-CLONAGE (blocs 4 et 8)
                // Le mobile a lu les données des blocs protégés, on vérifie leur intégrité
                _logger.LogInformation("[ACTIVATE-CHIP] Vérification authenticité puce {Uid}...", request.Uid);

                // Valider que les données sont présentes et au bon format
                if (string.IsNullOrWhiteSpace(request.ChipId) ||
                    string.IsNullOrWhiteSpace(request.Block4Data) ||
                    string.IsNullOrWhiteSpace(request.Block8Data))
                {
                    _logger.LogWarning("[ACTIVATE-CHIP] Données RFID manquantes pour puce {Uid}", request.Uid);
                    return BadRequest(new { message = "Données RFID incomplètes. Impossible de vérifier l'authenticité de la puce." });
                }

                // Vérifier format hexadécimal (32 caractères = 16 bytes)
                if (request.Block4Data.Length != 32 || request.Block8Data.Length != 32)
                {
                    _logger.LogWarning("[ACTIVATE-CHIP] Format données RFID invalide pour puce {Uid}", request.Uid);
                    return BadRequest(new { message = "Format données RFID invalide" });
                }

                // Vérifier que ChipId correspond à l'ID dans la base
                if (!Guid.TryParse(request.ChipId, out var scannedChipId) || scannedChipId != chip.Id)
                {
                    _logger.LogWarning("[ACTIVATE-CHIP] ⚠️ ALERTE SÉCURITÉ: ChipId bloc1 ne correspond pas à l'UID {Uid} (BDD: {DbChipId}, Scanned: {ScannedChipId})",
                        request.Uid, chip.Id, request.ChipId);
                    return BadRequest(new { message = "Puce invalide ou clonée (ChipId bloc1 incorrect)" });
                }

                // Vérifier que bloc 4 contient le même ChipId (protection anti-clonage niveau 1)
                byte[] block4Bytes;
                try
                {
                    block4Bytes = Convert.FromHexString(request.Block4Data);
                }
                catch
                {
                    _logger.LogWarning("[ACTIVATE-CHIP] Données bloc 4 non hexadécimales pour puce {Uid}", request.Uid);
                    return BadRequest(new { message = "Données bloc 4 invalides" });
                }

                var block4ChipId = System.Text.Encoding.ASCII.GetString(block4Bytes).TrimEnd('\0');
                if (block4ChipId != chip.Id.ToString())
                {
                    _logger.LogWarning("[ACTIVATE-CHIP] ⚠️ ALERTE SÉCURITÉ: ChipId bloc4 ne correspond pas (BDD: {DbChipId}, Bloc4: {Block4ChipId})",
                        chip.Id, block4ChipId);
                    return BadRequest(new { message = "Puce invalide ou clonée (ChipId bloc4 incorrect)" });
                }

                // Vérifier le checksum HMAC (protection anti-clonage niveau 2)
                if (chip.Salt == null || chip.Checksum == null)
                {
                    _logger.LogWarning("[ACTIVATE-CHIP] Puce {Uid} n'a pas été encodée correctement (Salt ou Checksum manquant)", request.Uid);
                    return BadRequest(new { message = "Cette puce n'a pas été encodée correctement" });
                }

                byte[] block8Bytes;
                try
                {
                    block8Bytes = Convert.FromHexString(request.Block8Data);
                }
                catch
                {
                    _logger.LogWarning("[ACTIVATE-CHIP] Données bloc 8 non hexadécimales pour puce {Uid}", request.Uid);
                    return BadRequest(new { message = "Données bloc 8 invalides" });
                }

                var block8Hex = BitConverter.ToString(block8Bytes).Replace("-", "").ToUpper();
                var expectedChecksum = chip.Checksum.ToUpper();

                if (block8Hex != expectedChecksum)
                {
                    _logger.LogWarning("[ACTIVATE-CHIP] ⚠️ ALERTE SÉCURITÉ: Checksum bloc8 invalide pour puce {Uid} (Attendu: {Expected}, Lu: {Actual})",
                        request.Uid, expectedChecksum, block8Hex);
                    return BadRequest(new { message = "Puce invalide ou clonée (Checksum anti-clonage incorrect)" });
                }

                _logger.LogInformation("[ACTIVATE-CHIP] ✅ Authenticité vérifiée pour puce {Uid} (Bloc1✓ Bloc4✓ Bloc8✓)", request.Uid);

                // 3. Trouver une commande DELIVERED du client avec des slots libres (FIFO - la plus ancienne)
                var availableOrder = await _context.Orders
                    .Where(o => o.CustomerId == customerId && o.Status == "DELIVERED")
                    .Select(o => new {
                        Order = o,
                        AssignedCount = _context.RfidChips.Count(c => c.OrderId == o.Id)
                    })
                    .Where(x => x.AssignedCount < x.Order.ChipsQuantity)
                    .OrderBy(x => x.Order.DeliveredAt) // FIFO: la plus ancienne d'abord
                    .FirstOrDefaultAsync();

                if (availableOrder == null)
                {
                    _logger.LogWarning("[ACTIVATE-CHIP] Client {CustomerId} n'a aucune commande DELIVERED avec des slots libres", customerId);
                    return BadRequest(new {
                        message = "Aucune commande livrée avec des slots disponibles. Avez-vous confirmé la réception de votre colis avec le code PKG ?"
                    });
                }

                var order = availableOrder.Order;

                // 4. Vérifier l'abonnement du client
                var customer = await _context.Customers.FindAsync(customerId);
                if (customer == null)
                    return NotFound(new { message = "Client introuvable" });

                if (!customer.IsActive)
                {
                    _logger.LogWarning("[ACTIVATE-CHIP] Client {CustomerId} a un abonnement inactif", customerId);
                    return BadRequest(new { message = "Votre abonnement n'est pas actif" });
                }

                // Vérifier le nombre total de puces actives du client vs limite abonnement
                var totalActiveChips = await _context.RfidChips
                    .CountAsync(c => c.CustomerId == customerId && (c.Status == "INACTIVE" || c.Status == "ACTIVE"));

                // TODO: Ajouter une propriété MaxChips sur Customer pour la limite d'abonnement
                var subscriptionLimit = 1000; // Limite par défaut (à paramétrer)
                if (totalActiveChips >= subscriptionLimit)
                {
                    _logger.LogWarning("[ACTIVATE-CHIP] Client {CustomerId} a atteint la limite d'abonnement ({Total}/{Limit})",
                        customerId, totalActiveChips, subscriptionLimit);
                    return BadRequest(new { message = $"Limite d'abonnement atteinte ({totalActiveChips}/{subscriptionLimit} puces)" });
                }

                // 5. ASSIGNER LA PUCE
                chip.CustomerId = customerId;
                chip.OrderId = order.Id;
                chip.Status = "INACTIVE";
                chip.FirstScanDate = DateTime.UtcNow;
                chip.LastScanDate = DateTime.UtcNow;
                chip.UpdatedAt = DateTime.UtcNow;

                // Enregistrer l'historique de statut
                var historyEntry = new RfidChipStatusHistory
                {
                    Id = Guid.NewGuid(),
                    RfidChipId = chip.Id,
                    FromStatus = "EN_STOCK",
                    ToStatus = "INACTIVE",
                    ChangedAt = DateTime.UtcNow,
                    ChangedBy = customerId,
                    Notes = $"Activation par scan client (Order: {order.OrderNumber})"
                };
                _context.RfidChipStatusHistory.Add(historyEntry);

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "[ACTIVATE-CHIP] ✅ Puce {Uid} activée pour client {CustomerId}, assignée à Order {OrderNumber}",
                    request.Uid, customerId, order.OrderNumber
                );

                // Vérifier si la commande est complète et libérer la réservation de stock
                var assignedCount = await _context.RfidChips.CountAsync(c => c.OrderId == order.Id);
                if (order.IsStockReserved && assignedCount >= order.ChipsQuantity)
                {
                    order.IsStockReserved = false;
                    order.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "[ACTIVATE-CHIP] Commande {OrderNumber} complète ({Assigned}/{Total}), réservation stock libérée",
                        order.OrderNumber, assignedCount, order.ChipsQuantity
                    );
                }

                return Ok(new
                {
                    message = "Puce activée avec succès",
                    chipId = chip.ChipId,
                    uid = chip.Uid,
                    status = chip.Status,
                    orderId = order.Id,
                    orderNumber = order.OrderNumber,
                    assignedCount = assignedCount,
                    totalQuantity = order.ChipsQuantity,
                    remainingSlots = order.ChipsQuantity - assignedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ACTIVATE-CHIP] ❌ Erreur lors de l'activation de la puce {Uid}", request.Uid);
                return StatusCode(500, new { message = "Erreur serveur lors de l'activation", error = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/rfidchips/request-encoding
        /// L'application WPF soumet l'UID → Backend vérifie et génère les paramètres d'encodage
        /// Workflow: Vérifie que la puce est EN_ATELIER → Génère Salt, ChipId, Checksum, ChipKey
        /// </summary>
        [HttpPost("request-encoding")]
        [AllowAnonymous] // L'application WPF n'a pas d'auth (machine locale atelier)
        public async Task<ActionResult<EncodingParametersResponse>> RequestEncoding(RequestEncodingRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Uid))
                {
                    return BadRequest(new { message = "L'UID est obligatoire" });
                }

                var trimmedUid = request.Uid.Trim();
                _logger.LogInformation($"📡 [REQUEST-ENCODING] UID reçu: {trimmedUid}");

                // 1. Vérifier que l'UID existe en base de données
                var chip = await _context.RfidChips
                    .FirstOrDefaultAsync(c => c.Uid == trimmedUid);

                if (chip == null)
                {
                    _logger.LogWarning($"⚠️ [REQUEST-ENCODING] UID inconnu: {trimmedUid}");
                    return NotFound(new EncodingParametersResponse
                    {
                        Message = "Puce inconnue - UID non enregistré en base de données"
                    });
                }

                // 2. Vérifier que la puce est au statut EN_ATELIER
                if (chip.Status != "EN_ATELIER")
                {
                    _logger.LogWarning($"⚠️ [REQUEST-ENCODING] Puce {trimmedUid} n'est pas EN_ATELIER (statut: {chip.Status})");
                    return BadRequest(new EncodingParametersResponse
                    {
                        Message = $"Puce non disponible pour encodage (statut: {chip.Status}). Elle doit être EN_ATELIER."
                    });
                }

                // 3. Vérifier que la puce n'est pas déjà encodée
                if (!string.IsNullOrEmpty(chip.Salt) && !string.IsNullOrEmpty(chip.Checksum))
                {
                    _logger.LogWarning($"⚠️ [REQUEST-ENCODING] Puce {trimmedUid} déjà encodée (ChipId: {chip.ChipId})");
                    return BadRequest(new EncodingParametersResponse
                    {
                        ChipId = chip.ChipId,
                        Message = "Cette puce est déjà encodée"
                    });
                }

                // 4. Générer les paramètres d'encodage
                var salt = _rfidSecurityService.GenerateSalt();
                var chipId = chip.ChipId; // ChipId déjà généré lors de l'import
                var checksum = _rfidSecurityService.GenerateChecksum(trimmedUid, salt, chipId);
                var chipKey = _rfidSecurityService.GenerateChipKey(chipId);

                // 5. Enregistrer les paramètres en base de données
                chip.Salt = salt;
                chip.Checksum = checksum;
                chip.EncodingDate = DateTime.UtcNow;
                chip.Status = "EN_STOCK";  // Puce encodée, prête à expédier
                chip.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ [REQUEST-ENCODING] Paramètres générés pour {trimmedUid} (ChipId: {chipId})");

                return Ok(new EncodingParametersResponse
                {
                    ChipId = chipId,
                    Salt = salt,
                    Checksum = checksum,
                    ChipKey = chipKey,
                    Message = "Paramètres d'encodage générés avec succès"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ [REQUEST-ENCODING] Erreur pour UID: {request.Uid}");
                return StatusCode(500, new { message = "Erreur serveur lors de la génération des paramètres d'encodage" });
            }
        }

        /// <summary>
        /// GET /api/rfidchips/info/{uid}
        /// Récupère les informations d'une puce déjà encodée
        /// Utilisé par l'application WPF pour afficher les infos d'une puce déjà codée
        /// </summary>
        [HttpGet("info/{uid}")]
        [AllowAnonymous] // L'application WPF n'a pas d'auth (machine locale atelier)
        public async Task<ActionResult<ChipInfoResponse>> GetChipInfo(string uid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(uid))
                {
                    return BadRequest(new { message = "L'UID est obligatoire" });
                }

                var trimmedUid = uid.Trim();
                _logger.LogInformation($"📡 [CHIP-INFO] UID reçu: {trimmedUid}");

                // Récupérer la puce avec son point de contrôle
                var chip = await _context.RfidChips
                    .Include(c => c.ControlPoint)
                    .FirstOrDefaultAsync(c => c.Uid == trimmedUid);

                if (chip == null)
                {
                    _logger.LogWarning($"⚠️ [CHIP-INFO] UID inconnu: {trimmedUid}");
                    return Ok(new ChipInfoResponse
                    {
                        IsEncoded = false,
                        Message = "Puce inconnue"
                    });
                }

                // Vérifier si la puce est encodée (a un Salt et Checksum)
                var isEncoded = !string.IsNullOrEmpty(chip.Salt) && !string.IsNullOrEmpty(chip.Checksum);

                _logger.LogInformation($"✅ [CHIP-INFO] Puce trouvée: {chip.ChipId} (Status: {chip.Status}, Encodée: {isEncoded})");

                return Ok(new ChipInfoResponse
                {
                    IsEncoded = isEncoded,
                    ChipId = chip.ChipId,
                    Status = chip.Status,
                    CustomerId = chip.CustomerId,
                    ControlPointId = chip.ControlPointId,
                    ControlPointName = chip.ControlPoint?.Name,
                    Message = isEncoded ? "Puce encodée" : "Puce non encodée"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ [CHIP-INFO] Erreur pour UID: {uid}");
                return StatusCode(500, new { message = "Erreur serveur lors de la récupération des informations" });
            }
        }

        /// <summary>
        /// POST /api/rfidchips/fix-fake-encoded
        /// CORRECTION URGENTE : Réinitialise les puces "faussement encodées" par la simulation
        /// Remet EN_ATELIER les puces EN_STOCK avec Salt/Checksum mais jamais encodées physiquement
        /// </summary>
        [HttpPost("fix-fake-encoded")]
        [Authorize(Roles = "SUPERADMIN")] // Seulement SUPERADMIN
        public async Task<ActionResult> FixFakeEncodedChips()
        {
            try
            {
                _logger.LogWarning("🔧 [FIX-FAKE-ENCODED] Début correction des puces faussement encodées");

                // 1. Trouver toutes les puces EN_STOCK avec Salt/Checksum
                var fakeEncodedChips = await _context.RfidChips
                    .Where(c => c.Status == "EN_STOCK" && c.Salt != null && c.Checksum != null)
                    .ToListAsync();

                _logger.LogWarning($"⚠️ [FIX-FAKE-ENCODED] {fakeEncodedChips.Count} puces trouvées EN_STOCK avec Salt/Checksum");

                if (fakeEncodedChips.Count == 0)
                {
                    return Ok(new
                    {
                        message = "Aucune puce à corriger",
                        correctedCount = 0
                    });
                }

                // 2. Réinitialiser chaque puce
                foreach (var chip in fakeEncodedChips)
                {
                    var oldStatus = chip.Status;
                    chip.Status = "EN_ATELIER";
                    chip.Salt = null;
                    chip.Checksum = null;
                    chip.EncodingDate = null;
                    chip.UpdatedAt = DateTime.UtcNow;

                    // Enregistrer dans l'historique
                    var historyEntry = new RfidChipStatusHistory
                    {
                        Id = Guid.NewGuid(),
                        RfidChipId = chip.Id,
                        FromStatus = oldStatus,
                        ToStatus = "EN_ATELIER",
                        ChangedAt = DateTime.UtcNow,
                        ChangedBy = GetUserId(),
                        Notes = "Correction: Puce faussement encodée par simulation - Réinitialisée pour encodage réel"
                    };
                    _context.RfidChipStatusHistory.Add(historyEntry);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ [FIX-FAKE-ENCODED] {fakeEncodedChips.Count} puces corrigées et remises EN_ATELIER");

                return Ok(new
                {
                    message = $"{fakeEncodedChips.Count} puces corrigées avec succès",
                    correctedCount = fakeEncodedChips.Count,
                    details = "Puces remises EN_ATELIER, Salt/Checksum/EncodingDate effacés"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [FIX-FAKE-ENCODED] Erreur lors de la correction");
                return StatusCode(500, new { message = "Erreur lors de la correction", error = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/rfidchips/whitelist/{customerId}
        /// WHITELIST MOBILE : Récupère toutes les puces ACTIVE du client pour validation offline
        /// </summary>
        [HttpGet("whitelist/{customerId}")]
        [Authorize]
        public async Task<ActionResult> GetWhitelist(Guid customerId)
        {
            try
            {
                // Vérifier que le customerId du token correspond au customerId demandé (sécurité)
                var tokenCustomerId = GetCustomerId();

                // SUPERADMIN peut voir n'importe quelle whitelist, sinon vérifier ownership
                if (!User.IsInRole("SUPERADMIN") && tokenCustomerId != customerId)
                {
                    _logger.LogWarning($"⚠️ [WHITELIST] Tentative accès whitelist non autorisée: User {tokenCustomerId} → Customer {customerId}");
                    return Forbid();
                }

                _logger.LogInformation($"📋 [WHITELIST] Récupération whitelist pour customer: {customerId}");

                // Récupérer toutes les puces ACTIVE du client avec leur point de contrôle
                var chips = await _context.RfidChips
                    .Where(c => c.CustomerId == customerId && c.Status == "ACTIVE")
                    .Include(c => c.ControlPoint)
                    .Select(c => new
                    {
                        chipId = c.Id.ToString(),
                        controlPointId = c.ControlPointId.HasValue ? c.ControlPointId.ToString() : null,
                        controlPointName = c.ControlPoint != null ? c.ControlPoint.Name : null,
                        activatedAt = c.FirstScanDate,
                        status = c.Status
                    })
                    .ToListAsync();

                _logger.LogInformation($"✅ [WHITELIST] {chips.Count} puces actives trouvées pour customer {customerId}");

                return Ok(new
                {
                    customerId = customerId.ToString(),
                    chips,
                    lastUpdated = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ [WHITELIST] Erreur récupération whitelist pour customer: {customerId}");
                return StatusCode(500, new { message = "Erreur serveur lors de la récupération de la whitelist" });
            }
        }
    }
}
