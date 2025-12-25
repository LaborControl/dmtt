using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.Models;
using LaborControl.API.DTOs;

namespace LaborControl.API.Controllers
{
    [ApiController]
    [Route("api/supplier-orders")]
    [Authorize]
    public class SupplierOrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SupplierOrdersController> _logger;

        public SupplierOrdersController(
            ApplicationDbContext context,
            ILogger<SupplierOrdersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/supplier-orders
        /// Récupère toutes les commandes fournisseurs
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<SupplierOrderResponse>>> GetSupplierOrders(
            [FromQuery] string? status = null,
            [FromQuery] Guid? supplierId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _context.SupplierOrders
                    .Include(o => o.Lines)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(o => o.Status == status);
                }

                if (supplierId.HasValue)
                {
                    query = query.Where(o => o.SupplierId == supplierId.Value);
                }

                var orders = await query
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(o => new SupplierOrderResponse
                    {
                        Id = o.Id,
                        SupplierId = o.SupplierId,
                        OrderNumber = o.OrderNumber,
                        OrderDate = o.OrderDate,
                        SentDate = o.SentDate,
                        ExpectedDeliveryDate = o.ExpectedDeliveryDate,
                        ReceivedDate = o.ReceivedDate,
                        TotalAmount = o.TotalAmount,
                        Status = o.Status,
                        Notes = o.Notes,
                        Lines = o.Lines.Select(l => new SupplierOrderLineResponse
                        {
                            Id = l.Id,
                            ProductType = l.ProductType,
                            Quantity = l.Quantity,
                            ReceivedQuantity = l.ReceivedQuantity,
                            UnitPrice = l.UnitPrice,
                            TotalPrice = l.TotalPrice
                        }).ToList(),
                        CreatedAt = o.CreatedAt,
                        UpdatedAt = o.UpdatedAt
                    })
                    .ToListAsync();

                _logger.LogInformation($"✅ {orders.Count} commandes fournisseurs récupérées");
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur GetSupplierOrders");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/supplier-orders/{id}
        /// Récupère une commande fournisseur spécifique
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<SupplierOrderResponse>> GetSupplierOrder(Guid id)
        {
            try
            {
                var order = await _context.SupplierOrders
                    .Include(o => o.Lines)
                    .Where(o => o.Id == id)
                    .Select(o => new SupplierOrderResponse
                    {
                        Id = o.Id,
                        SupplierId = o.SupplierId,
                        OrderNumber = o.OrderNumber,
                        OrderDate = o.OrderDate,
                        SentDate = o.SentDate,
                        ExpectedDeliveryDate = o.ExpectedDeliveryDate,
                        ReceivedDate = o.ReceivedDate,
                        TotalAmount = o.TotalAmount,
                        Status = o.Status,
                        Notes = o.Notes,
                        Lines = o.Lines.Select(l => new SupplierOrderLineResponse
                        {
                            Id = l.Id,
                            ProductType = l.ProductType,
                            Quantity = l.Quantity,
                            ReceivedQuantity = l.ReceivedQuantity,
                            UnitPrice = l.UnitPrice,
                            TotalPrice = l.TotalPrice
                        }).ToList(),
                        CreatedAt = o.CreatedAt,
                        UpdatedAt = o.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    return NotFound(new { message = "Commande fournisseur non trouvée" });
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur GetSupplierOrder {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/supplier-orders
        /// Crée une nouvelle commande fournisseur
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<SupplierOrderResponse>> CreateSupplierOrder(CreateSupplierOrderRequest request)
        {
            try
            {
                // Vérifier que le fournisseur existe
                var supplier = await _context.Suppliers.FindAsync(request.SupplierId);
                if (supplier == null)
                {
                    return BadRequest(new { message = "Fournisseur non trouvé" });
                }

                // Vérifier que le numéro de commande est unique
                var existingOrder = await _context.SupplierOrders
                    .FirstOrDefaultAsync(o => o.OrderNumber == request.OrderNumber);
                if (existingOrder != null)
                {
                    return Conflict(new { message = "Ce numéro de commande existe déjà" });
                }

                var order = new SupplierOrder
                {
                    Id = Guid.NewGuid(),
                    SupplierId = request.SupplierId,
                    OrderNumber = request.OrderNumber,
                    OrderDate = DateTime.UtcNow,
                    ExpectedDeliveryDate = request.ExpectedDeliveryDate?.ToUniversalTime(),
                    TotalAmount = request.TotalAmount,
                    Status = "DRAFT",
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Ajouter les lignes
                foreach (var lineRequest in request.Lines)
                {
                    var line = new SupplierOrderLine
                    {
                        Id = Guid.NewGuid(),
                        SupplierOrderId = order.Id,
                        ProductType = lineRequest.ProductType,
                        Quantity = lineRequest.Quantity,
                        UnitPrice = lineRequest.UnitPrice,
                        TotalPrice = lineRequest.Quantity * lineRequest.UnitPrice,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    order.Lines.Add(line);
                }

                _context.SupplierOrders.Add(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Commande fournisseur créée: {order.OrderNumber}");

                return CreatedAtAction(nameof(GetSupplierOrder), new { id = order.Id }, MapToResponse(order));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur CreateSupplierOrder");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/supplier-orders/{id}
        /// Modifie une commande fournisseur (DRAFT uniquement)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSupplierOrder(Guid id, UpdateSupplierOrderRequest request)
        {
            try
            {
                var order = await _context.SupplierOrders.FindAsync(id);

                if (order == null)
                {
                    return NotFound(new { message = "Commande fournisseur non trouvée" });
                }

                if (order.Status != "DRAFT")
                {
                    return BadRequest(new { message = "Seules les commandes en DRAFT peuvent être modifiées" });
                }

                order.ExpectedDeliveryDate = request.ExpectedDeliveryDate?.ToUniversalTime();
                order.Notes = request.Notes;
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Commande fournisseur modifiée: {order.OrderNumber}");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur UpdateSupplierOrder {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/supplier-orders/{id}/send
        /// Envoie une commande fournisseur (DRAFT → SENT)
        /// </summary>
        [HttpPost("{id}/send")]
        public async Task<IActionResult> SendSupplierOrder(Guid id, SendSupplierOrderRequest request)
        {
            try
            {
                var order = await _context.SupplierOrders.FindAsync(id);

                if (order == null)
                {
                    return NotFound(new { message = "Commande fournisseur non trouvée" });
                }

                if (order.Status != "DRAFT")
                {
                    return BadRequest(new { message = $"Transition invalide: {order.Status} → SENT" });
                }

                order.Status = "SENT";
                order.SentDate = DateTime.UtcNow;
                if (request.ExpectedDeliveryDate.HasValue)
                {
                    order.ExpectedDeliveryDate = request.ExpectedDeliveryDate?.ToUniversalTime();
                }
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Commande fournisseur envoyée: {order.OrderNumber}");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur SendSupplierOrder {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/supplier-orders/{id}/receive
        /// Réceptionne une commande fournisseur (SENT → EN_TRANSIT → RECEIVED)
        /// Transition des puces RFID : EN_TRANSIT → EN_ATELIER
        /// ⚠️ VALIDATION : Impossible de réceptionner sans puces importées
        /// </summary>
        [HttpPost("{id}/receive")]
        public async Task<IActionResult> ReceiveSupplierOrder(Guid id, ReceiveSupplierOrderRequest request)
        {
            try
            {
                var order = await _context.SupplierOrders
                    .Include(o => o.Lines)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return NotFound(new { message = "Commande fournisseur non trouvée" });
                }

                if (order.Status != "SENT" && order.Status != "EN_TRANSIT")
                {
                    return BadRequest(new { message = $"Transition invalide: {order.Status} → RECEIVED" });
                }

                // ⚠️ VALIDATION : Vérifier que des puces ont été importées
                var importedChipsCount = await _context.RfidChips
                    .CountAsync(c => c.SupplierOrderId == id);

                if (importedChipsCount == 0)
                {
                    _logger.LogWarning($"⚠️ Tentative de réception sans puces importées: {order.OrderNumber}");
                    return BadRequest(new
                    {
                        message = "Impossible de réceptionner : aucune puce RFID importée",
                        action = "Importez les UIDs des puces avant de réceptionner la commande",
                        orderNumber = order.OrderNumber
                    });
                }

                // Mettre à jour les quantités reçues
                foreach (var lineRequest in request.Lines)
                {
                    var line = order.Lines.FirstOrDefault(l => l.Id == lineRequest.LineId);
                    if (line != null)
                    {
                        line.ReceivedQuantity = lineRequest.ReceivedQuantity;
                    }
                }

                // Transition des puces RFID : EN_TRANSIT → EN_ATELIER
                var chips = await _context.RfidChips
                    .Where(c => c.SupplierOrderId == id && c.Status == "EN_TRANSIT")
                    .ToListAsync();

                foreach (var chip in chips)
                {
                    chip.Status = "EN_ATELIER";
                    chip.ReceivedFromSupplierDate = DateTime.UtcNow;
                    chip.UpdatedAt = DateTime.UtcNow;
                }

                order.Status = "RECEIVED";
                order.ReceivedDate = DateTime.UtcNow;
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Commande fournisseur reçue: {order.OrderNumber} - {chips.Count} puces transférées en EN_ATELIER");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur ReceiveSupplierOrder {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/supplier-orders/{id}/rollback-to-sent
        /// Rétrograde une commande RECEIVED → SENT
        /// Réinitialise ReceivedDate et ReceivedQuantity
        /// ⚠️ Utilisé pour corriger les erreurs de workflow
        /// </summary>
        [HttpPut("{id}/rollback-to-sent")]
        public async Task<IActionResult> RollbackToSent(Guid id)
        {
            try
            {
                var order = await _context.SupplierOrders
                    .Include(o => o.Lines)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return NotFound(new { message = "Commande fournisseur non trouvée" });
                }

                if (order.Status != "RECEIVED")
                {
                    return BadRequest(new { message = $"Seules les commandes RECEIVED peuvent être rétrogradées (statut actuel: {order.Status})" });
                }

                // Rétrograder la commande
                order.Status = "SENT";
                order.ReceivedDate = null;
                order.UpdatedAt = DateTime.UtcNow;

                // Réinitialiser les quantités reçues
                foreach (var line in order.Lines)
                {
                    line.ReceivedQuantity = 0;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Commande fournisseur rétrogradée: {order.OrderNumber} (RECEIVED → SENT)");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur RollbackToSent {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// DELETE /api/supplier-orders/{id}
        /// Annule une commande fournisseur (DRAFT uniquement)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelSupplierOrder(Guid id)
        {
            try
            {
                var order = await _context.SupplierOrders.FindAsync(id);

                if (order == null)
                {
                    return NotFound(new { message = "Commande fournisseur non trouvée" });
                }

                if (order.Status != "DRAFT")
                {
                    return BadRequest(new { message = "Seules les commandes en DRAFT peuvent être annulées" });
                }

                order.Status = "CANCELLED";
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Commande fournisseur annulée: {order.OrderNumber}");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur CancelSupplierOrder {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Helper pour mapper SupplierOrder vers SupplierOrderResponse
        /// </summary>
        private SupplierOrderResponse MapToResponse(SupplierOrder order)
        {
            return new SupplierOrderResponse
            {
                Id = order.Id,
                SupplierId = order.SupplierId,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                SentDate = order.SentDate,
                ExpectedDeliveryDate = order.ExpectedDeliveryDate,
                ReceivedDate = order.ReceivedDate,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                Notes = order.Notes,
                Lines = order.Lines.Select(l => new SupplierOrderLineResponse
                {
                    Id = l.Id,
                    ProductType = l.ProductType,
                    Quantity = l.Quantity,
                    ReceivedQuantity = l.ReceivedQuantity,
                    UnitPrice = l.UnitPrice,
                    TotalPrice = l.TotalPrice
                }).ToList(),
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt
            };
        }
    }
}
