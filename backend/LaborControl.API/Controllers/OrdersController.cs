using LaborControl.API.Data;
using LaborControl.API.DTOs;
using LaborControl.API.Models;
using LaborControl.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaborControl.API.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly StripePaymentService _stripeService;
        private readonly OrderFulfillmentService _fulfillmentService;
        private readonly IEmailService _emailService;
        private readonly StockManagementService _stockService;
        private readonly IDeliveryNoteService _deliveryNoteService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            ApplicationDbContext context,
            StripePaymentService stripeService,
            OrderFulfillmentService fulfillmentService,
            IEmailService emailService,
            StockManagementService stockService,
            IDeliveryNoteService deliveryNoteService,
            ILogger<OrdersController> logger)
        {
            _context = context;
            _stripeService = stripeService;
            _fulfillmentService = fulfillmentService;
            _emailService = emailService;
            _stockService = stockService;
            _deliveryNoteService = deliveryNoteService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<OrderResponse>> CreateOrder(CreateOrderRequest request)
        {
            // Récupérer le CustomerId depuis le token JWT
            var customerIdClaim = User.Claims.FirstOrDefault(c => c.Type == "CustomerId");
            if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return Unauthorized(new { message = "Token invalide" });
            }

            // Récupérer le customer
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
            {
                return NotFound(new { message = "Client introuvable" });
            }

            // Vérifier si le client n'a pas déjà commandé ses 10 puces gratuites
            var existingOrder = await _context.Orders
                .FirstOrDefaultAsync(o => o.CustomerId == customerId);

            if (existingOrder != null)
            {
                return BadRequest(new { message = "Vous avez déjà commandé votre pack découverte" });
            }

            // Générer un numéro de commande unique
            var orderNumber = GenerateOrderNumber();

            // Créer la commande
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                OrderNumber = orderNumber,
                ChipsQuantity = 10,
                TotalAmount = 10.00m,
                Status = "PENDING",
                DeliveryAddress = request.DeliveryAddress,
                DeliveryCity = request.DeliveryCity,
                DeliveryPostalCode = request.DeliveryPostalCode,
                DeliveryCountry = request.DeliveryCountry ?? "France",
                Service = request.Service,
                CreatedAt = DateTime.UtcNow,
                Notes = request.Notes
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Envoyer l'email de confirmation de commande
            try
            {
                var emailSent = await _emailService.SendOrderConfirmationEmailAsync(
                    customer.ContactEmail,
                    customer.Name,
                    order.OrderNumber,
                    order.TotalAmount
                );
                if (emailSent)
                {
                    _logger.LogInformation($"[EMAIL] Email de confirmation de commande envoyé à {customer.ContactEmail} (Commande: {order.OrderNumber})");
                }
                else
                {
                    _logger.LogWarning($"[EMAIL] Échec de l'envoi de l'email de confirmation à {customer.ContactEmail}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[EMAIL] Erreur lors de l'envoi de l'email de confirmation à {customer.ContactEmail}");
            }

            // Envoyer la notification à l'admin
            try
            {
                var adminNotificationSent = await _emailService.SendAdminNewOrderNotificationAsync(
                    customer.Name,
                    order.OrderNumber,
                    order.TotalAmount,
                    order.ChipsQuantity
                );
                if (adminNotificationSent)
                {
                    _logger.LogInformation($"[ADMIN EMAIL] Notification nouvelle commande envoyée pour {order.OrderNumber}");
                }
                else
                {
                    _logger.LogWarning($"[ADMIN EMAIL] Échec de l'envoi de la notification nouvelle commande pour {order.OrderNumber}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[ADMIN EMAIL] Erreur lors de l'envoi de la notification nouvelle commande pour {order.OrderNumber}");
            }

            return Ok(new OrderResponse
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerId = order.CustomerId,
                CustomerName = customer.Name,
                ChipsQuantity = order.ChipsQuantity,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                PackagingCode = order.PackagingCode,
                DeliveryAddress = order.DeliveryAddress,
                DeliveryCity = order.DeliveryCity,
                DeliveryPostalCode = order.DeliveryPostalCode,
                DeliveryCountry = order.DeliveryCountry,
                CreatedAt = order.CreatedAt,
                Notes = order.Notes
            });
        }

        [HttpPost("checkout")]
        [Authorize]
        public async Task<ActionResult<CreateCheckoutSessionResponse>> CreateCheckoutSession(CreateCheckoutSessionRequest request)
        {
            // Récupérer la commande
            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId);

            if (order == null)
            {
                return NotFound(new { message = "Commande introuvable" });
            }

            // Vérifier que la commande appartient à l'utilisateur
            var customerIdClaim = User.Claims.FirstOrDefault(c => c.Type == "CustomerId");
            if (customerIdClaim == null || order.CustomerId.ToString() != customerIdClaim.Value)
            {
                return Forbid();
            }

            // Vérifier que la commande n'est pas déjà payée
            if (order.Status == "PAID")
            {
                return BadRequest(new { message = "Cette commande a déjà été payée" });
            }

            // Créer la session Stripe
            var result = await _stripeService.CreateCheckoutSessionAsync(
                order.OrderNumber,
                order.TotalAmount,
                order.Customer?.ContactEmail ?? "",
                request.SuccessUrl,
                request.CancelUrl
            );

            if (!result.Success)
            {
                return BadRequest(new CreateCheckoutSessionResponse
                {
                    Success = false,
                    ErrorMessage = result.ErrorMessage
                });
            }

            // Enregistrer le session ID
            order.StripeCheckoutSessionId = result.SessionId;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new CreateCheckoutSessionResponse
            {
                Success = true,
                CheckoutUrl = result.CheckoutUrl,
                SessionId = result.SessionId
            });
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<OrderResponse>> GetOrder(Guid id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound(new { message = "Commande introuvable" });
            }

            // Vérifier les permissions : STAFF peut accéder à toutes les commandes, CLIENT uniquement aux siennes
            var userTypeClaim = User.Claims.FirstOrDefault(c => c.Type == "UserType");
            var isStaff = userTypeClaim?.Value == "STAFF";

            if (!isStaff)
            {
                // Pour les clients, vérifier que la commande leur appartient
                var customerIdClaim = User.Claims.FirstOrDefault(c => c.Type == "CustomerId");
                if (customerIdClaim == null || order.CustomerId.ToString() != customerIdClaim.Value)
                {
                    return Forbid();
                }
            }

            return Ok(new OrderResponse
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerId = order.CustomerId,
                CustomerName = order.Customer?.Name,
                ChipsQuantity = order.ChipsQuantity,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                PackagingCode = order.PackagingCode,
                DeliveryAddress = order.DeliveryAddress,
                DeliveryCity = order.DeliveryCity,
                DeliveryPostalCode = order.DeliveryPostalCode,
                DeliveryCountry = order.DeliveryCountry,
                StripeCheckoutSessionId = order.StripeCheckoutSessionId,
                TrackingNumber = order.TrackingNumber,
                ShippedAt = order.ShippedAt,
                DeliveredAt = order.DeliveredAt,
                CreatedAt = order.CreatedAt,
                Notes = order.Notes
            });
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<List<OrderResponse>>> GetMyOrders([FromQuery] string? status = null)
        {
            // Récupérer le CustomerId depuis le token JWT
            var customerIdClaim = User.Claims.FirstOrDefault(c => c.Type == "CustomerId");
            if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId) || customerId == Guid.Empty)
            {
                // Retourner une liste vide pour les utilisateurs sans customer (SUPERADMIN, etc.)
                return Ok(new List<OrderResponse>());
            }

            // Optimisation: AsNoTracking pour lecture seule
            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Where(o => o.CustomerId == customerId);

            // Filtre par statut si fourni
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderResponse
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer != null ? o.Customer.Name : null,
                    ChipsQuantity = o.ChipsQuantity,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    PackagingCode = o.PackagingCode,
                    DeliveryAddress = o.DeliveryAddress,
                    DeliveryCity = o.DeliveryCity,
                    DeliveryPostalCode = o.DeliveryPostalCode,
                    DeliveryCountry = o.DeliveryCountry,
                    TrackingNumber = o.TrackingNumber,
                    ShippedAt = o.ShippedAt,
                    DeliveredAt = o.DeliveredAt,
                    CreatedAt = o.CreatedAt,
                    CarrierName = o.CarrierName
                })
                .ToListAsync();

            return Ok(orders);
        }

        /// <summary>
        /// Récupère l'historique complet des commandes avec tous les statuts
        /// </summary>
        [HttpGet("details")]
        [Authorize]
        public async Task<ActionResult<List<OrderDetailResponse>>> GetMyOrdersWithDetails()
        {
            // Récupérer le CustomerId depuis le token JWT
            var customerIdClaim = User.Claims.FirstOrDefault(c => c.Type == "CustomerId");
            if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return Unauthorized(new { message = "Token invalide" });
            }

            var orders = await _context.Orders
                .Include(o => o.RfidChips)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var orderDetails = orders.Select(o =>
            {
                // Compter les puces activées (RECEIVED ou ACTIVE)
                var chipsActivated = o.RfidChips.Count(c => c.Status == "RECEIVED" || c.Status == "ACTIVE");
                var chipsTotal = o.RfidChips.Count;

                // Déterminer le statut d'activation
                string? activationStatus = null;
                if (chipsTotal > 0)
                {
                    if (chipsActivated == 0)
                        activationStatus = "PENDING";
                    else if (chipsActivated < chipsTotal)
                        activationStatus = "PARTIAL";
                    else
                        activationStatus = "COMPLETED";
                }

                // TODO: Compter les puces affectées à des points de contrôle
                var chipsAssigned = 0; // À implémenter quand on aura la relation RfidChip -> ControlPoint

                return new OrderDetailResponse
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    Status = o.Status,
                    ProductType = o.ProductType,
                    ChipsQuantity = o.ChipsQuantity,
                    TotalAmount = o.TotalAmount,
                    DeliveryAddress = o.DeliveryAddress,
                    DeliveryCity = o.DeliveryCity,
                    DeliveryPostalCode = o.DeliveryPostalCode,
                    Service = o.Service,
                    Notes = o.Notes,
                    CreatedAt = o.CreatedAt,
                    StripePaymentIntentId = o.StripePaymentIntentId,
                    PackagingCode = o.PackagingCode,

                    // Statut de livraison basé sur les dates
                    DeliveryStatus = o.DeliveredAt != null ? "DELIVERED" :
                                   o.ShippedAt != null ? "SHIPPED" :
                                   o.Status == "PAID" ? "PENDING" : null,

                    // Statuts d'activation avec comptage réel
                    ChipsActivationStatus = activationStatus,
                    ChipsActivated = chipsActivated,

                    // Statuts d'affectation (TODO: à implémenter avec les points de contrôle)
                    ChipsAssignmentStatus = chipsTotal > 0 && chipsAssigned == 0 ? "PENDING" :
                                          chipsAssigned > 0 && chipsAssigned < chipsTotal ? "PARTIAL" :
                                          chipsAssigned == chipsTotal ? "COMPLETED" : null,
                    ChipsAssigned = chipsAssigned
                };
            }).ToList();

            return Ok(orderDetails);
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> StripeWebhook()
        {
            // TODO: Implémenter le webhook Stripe pour confirmation de paiement
            // Mettre à jour le statut de la commande à "PAID"
            return Ok();
        }

        /// <summary>
        /// [ADMIN] Démarrer la préparation d'une commande
        /// </summary>
        [HttpPost("{id}/start-preparation")]
        [Authorize]
        public async Task<IActionResult> StartPreparation(Guid id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound(new { message = "Commande introuvable" });
            }

            if (order.Status != "PAID")
            {
                return BadRequest(new { message = "La commande doit être payée pour démarrer la préparation" });
            }

            order.Status = "PREPARING";
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Préparation démarrée", orderNumber = order.OrderNumber, status = order.Status });
        }

        /// <summary>
        /// [ADMIN] Marquer une commande comme prête à expédier
        /// </summary>
        [HttpPost("{id}/ready-to-ship")]
        [Authorize]
        public async Task<IActionResult> MarkReadyToShip(Guid id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound(new { message = "Commande introuvable" });
            }

            if (order.Status != "PREPARING")
            {
                return BadRequest(new { message = "La commande doit être en préparation" });
            }

            order.Status = "READY_TO_SHIP";
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Commande prête à expédier", orderNumber = order.OrderNumber, status = order.Status });
        }

        /// <summary>
        /// [ADMIN] Expédier une commande avec transporteur et numéro de suivi
        /// </summary>
        [HttpPost("{id}/ship")]
        [Authorize]
        public async Task<IActionResult> ShipOrder(Guid id, [FromBody] ShipOrderRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CarrierName) || string.IsNullOrWhiteSpace(request.TrackingNumber))
            {
                return BadRequest(new { message = "CarrierName et TrackingNumber requis" });
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound(new { message = "Commande introuvable" });
            }

            // Accepter les commandes PAID, PREPARING ou READY_TO_SHIP
            if (order.Status != "PAID" && order.Status != "PREPARING" && order.Status != "READY_TO_SHIP")
            {
                return BadRequest(new { message = $"La commande ne peut pas être expédiée depuis le statut {order.Status}" });
            }

            order.Status = "SHIPPED";
            order.CarrierName = request.CarrierName;
            order.TrackingNumber = request.TrackingNumber;
            order.ShippedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new {
                message = "Commande expédiée",
                orderNumber = order.OrderNumber,
                carrierName = order.CarrierName,
                trackingNumber = order.TrackingNumber,
                shippedAt = order.ShippedAt
            });
        }

        /// <summary>
        /// [ADMIN] Marquer une commande comme livrée
        /// </summary>
        [HttpPost("{id}/deliver")]
        [Authorize]
        public async Task<IActionResult> MarkDelivered(Guid id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound(new { message = "Commande introuvable" });
            }

            if (order.Status != "SHIPPED")
            {
                return BadRequest(new { message = "La commande doit être expédiée" });
            }

            order.Status = "DELIVERED";
            order.DeliveredAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new {
                message = "Commande livrée",
                orderNumber = order.OrderNumber,
                deliveredAt = order.DeliveredAt
            });
        }

        /// <summary>
        /// [ADMIN/TEST] Marquer une commande comme PAID
        /// </summary>
        [HttpPost("{id}/mark-paid")]
        [Authorize]
        public async Task<IActionResult> MarkOrderAsPaid(Guid id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound(new { message = "Commande introuvable" });
            }

            order.Status = "PAID";
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Commande marquée comme PAID", orderNumber = order.OrderNumber, status = order.Status });
        }

        /// <summary>
        /// [ADMIN/TEST] Générer les puces RFID pour une commande par numéro de commande
        /// </summary>
        [HttpPost("fulfill/{orderNumber}")]
        [Authorize]
        public async Task<IActionResult> FulfillOrderByNumber(string orderNumber)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
            if (order == null)
            {
                return NotFound(new { message = $"Commande {orderNumber} introuvable" });
            }

            var success = await _fulfillmentService.FulfillOrderAsync(order.Id);

            if (!success)
            {
                return BadRequest(new { message = "Impossible de traiter cette commande. Vérifiez qu'elle est payée et non déjà traitée." });
            }

            var updatedOrder = await _context.Orders
                .Include(o => o.RfidChips)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            return Ok(new
            {
                message = "✅ Commande traitée avec succès",
                orderNumber = updatedOrder?.OrderNumber,
                orderId = updatedOrder?.Id,
                packagingCode = updatedOrder?.PackagingCode,
                chipsGenerated = updatedOrder?.RfidChips.Count ?? 0,
                chipCodes = updatedOrder?.RfidChips.Take(3).Select(c => c.PackagingCode).ToList()
            });
        }

        /// <summary>
        /// [ADMIN/TEST] Générer les puces RFID pour une commande payée (Fulfillment)
        /// </summary>
        [HttpPost("{id}/fulfill")]
        [Authorize] // TODO: Ajouter [Authorize(Roles = "Admin")] en production
        public async Task<IActionResult> FulfillOrder(Guid id)
        {
            var success = await _fulfillmentService.FulfillOrderAsync(id);

            if (!success)
            {
                return BadRequest(new { message = "Impossible de traiter cette commande. Vérifiez qu'elle est payée et non déjà traitée." });
            }

            var order = await _context.Orders
                .Include(o => o.RfidChips)
                .FirstOrDefaultAsync(o => o.Id == id);

            return Ok(new
            {
                message = "Commande traitée avec succès",
                orderId = id,
                packagingCode = order?.PackagingCode,
                chipsGenerated = order?.RfidChips.Count ?? 0
            });
        }

        /// <summary>
        /// Réceptionner une commande avec le code packaging
        /// </summary>
        [HttpPost("{id}/receive")]
        [Authorize]
        public async Task<ActionResult<ReceiveOrderResponse>> ReceiveOrder(Guid id, [FromBody] ReceiveOrderRequest request)
        {
            // Récupérer le CustomerId depuis le token JWT
            var customerIdClaim = User.Claims.FirstOrDefault(c => c.Type == "CustomerId");
            if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return Unauthorized(new { message = "Token invalide" });
            }

            var (success, message, chipsReceived) = await _fulfillmentService.ReceiveOrderAsync(id, request.PackagingCode, customerId);

            if (!success)
            {
                return BadRequest(new { message });
            }

            return Ok(new ReceiveOrderResponse
            {
                Success = true,
                Message = message,
                ChipsReceived = chipsReceived
            });
        }

        private string GenerateOrderNumber()
        {
            // Format: LC-2025-XXXXXX
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"LC-{DateTime.UtcNow.Year}-{random}{timestamp.Substring(8, 4)}";
        }

        /// <summary>
        /// [ADMIN] Récupère TOUTES les commandes de tous les clients
        /// </summary>
        [HttpGet("admin/all")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<ActionResult<List<OrderResponse>>> GetAllOrdersForAdmin()
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderResponse
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer != null ? o.Customer.Name : "Inconnu",
                    ChipsQuantity = o.ChipsQuantity,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    PackagingCode = o.PackagingCode,
                    DeliveryAddress = o.DeliveryAddress,
                    DeliveryCity = o.DeliveryCity,
                    DeliveryPostalCode = o.DeliveryPostalCode,
                    DeliveryCountry = o.DeliveryCountry,
                    TrackingNumber = o.TrackingNumber,
                    ShippedAt = o.ShippedAt,
                    DeliveredAt = o.DeliveredAt,
                    CreatedAt = o.CreatedAt,
                    CarrierName = o.CarrierName
                })
                .ToListAsync();

            return Ok(orders);
        }

        /// <summary>
        /// Récupère les 10 dernières commandes de tous les clients
        /// </summary>
        [HttpGet("recent")]
        [AllowAnonymous]
        public async Task<ActionResult<List<RecentOrderDTO>>> GetRecentOrders([FromQuery] int limit = 10)
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.CreatedAt)
                .Take(limit)
                .Select(o => new RecentOrderDTO
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.Customer != null ? o.Customer.Name : "Inconnu",
                    ChipsQuantity = o.ChipsQuantity,
                    CreatedAt = o.CreatedAt,
                    Status = o.Status
                })
                .ToListAsync();

            return Ok(orders);
        }

        /// <summary>
        /// Préparer une commande - Marque le stock comme réservé (sans scan des puces)
        /// </summary>
        [HttpPost("{id}/prepare")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<ActionResult> PrepareOrder(Guid id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound(new { message = "Commande introuvable" });

            if (order.Status == "CANCELLED")
                return BadRequest(new { message = "Commande annulée" });

            if (order.IsStockReserved)
                return BadRequest(new { message = "Stock déjà réservé pour cette commande" });

            // Réserver le stock via le service
            var (success, message) = await _stockService.ReserveStockForOrderAsync(id);
            if (!success)
                return BadRequest(new { message });

            _logger.LogInformation("[ORDER] Commande {OrderId} préparée: stock réservé", id);

            return Ok(new
            {
                message,
                orderId = id,
                orderNumber = order.OrderNumber,
                quantity = order.ChipsQuantity,
                preparedAt = order.PreparedAt,
                availableStock = await _stockService.GetAvailableStockAsync()
            });
        }

        /// <summary>
        /// Client confirme la réception du colis en saisissant le PKG
        /// </summary>
        [HttpPost("receive-delivery")]
        [Authorize]
        public async Task<ActionResult> ReceiveDelivery([FromBody] ReceiveDeliveryRequest request)
        {
            // Récupérer le CustomerId depuis le token
            var customerIdClaim = User.Claims.FirstOrDefault(c => c.Type == "CustomerId");
            if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
                return Unauthorized(new { message = "Token invalide" });

            // Trouver l'order via PKG
            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.PackagingCode == request.Pkg);

            if (order == null)
                return NotFound(new { message = "PKG invalide ou commande introuvable" });

            if (order.CustomerId != customerId)
                return Forbid("Ce PKG n'appartient pas à ce client");

            if (order.Status == "DELIVERED")
                return BadRequest(new { message = "Livraison déjà confirmée" });

            if (order.Status != "SHIPPED")
                return BadRequest(new { message = "La commande n'est pas encore expédiée" });

            // Marquer la commande comme livrée
            order.Status = "DELIVERED";
            order.DeliveredAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "[ORDER] Livraison confirmée par client {CustomerId} pour Order {OrderId} (PKG: {Pkg})",
                customerId, order.Id, request.Pkg
            );

            return Ok(new
            {
                message = "Livraison confirmée avec succès",
                orderId = order.Id,
                orderNumber = order.OrderNumber,
                deliveredAt = order.DeliveredAt,
                chipsQuantity = order.ChipsQuantity,
                instructions = "Vous pouvez maintenant scanner vos puces RFID pour les activer"
            });
        }

        /// <summary>
        /// Obtenir les statistiques du stock RFID
        /// </summary>
        [HttpGet("stock/statistics")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<ActionResult<StockStatistics>> GetStockStatistics()
        {
            var stats = await _stockService.GetStockStatisticsAsync();
            return Ok(stats);
        }

        /// <summary>
        /// Générer le bon de livraison PDF pour une commande
        /// </summary>
        [HttpGet("{id}/delivery-note")]
        [Authorize]
        public async Task<ActionResult> GenerateDeliveryNote(Guid id)
        {
            try
            {
                // Récupérer la commande avec les informations du client
                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return NotFound(new { message = "Commande introuvable" });
                }

                // Vérifier les permissions : STAFF peut accéder à toutes les commandes, CLIENT uniquement aux siennes
                var userTypeClaim = User.Claims.FirstOrDefault(c => c.Type == "UserType");
                var isStaff = userTypeClaim?.Value == "STAFF";

                if (!isStaff)
                {
                    // Pour les clients, vérifier que la commande leur appartient
                    var customerIdClaim = User.Claims.FirstOrDefault(c => c.Type == "CustomerId");
                    if (customerIdClaim == null || order.CustomerId.ToString() != customerIdClaim.Value)
                    {
                        return Forbid();
                    }
                }

                // Vérifier que la commande a un code packaging (nécessaire pour le BL)
                if (string.IsNullOrEmpty(order.PackagingCode))
                {
                    return BadRequest(new { message = "La commande n'a pas encore de code packaging. Veuillez d'abord générer le code packaging." });
                }

                // Vérifier que la commande a un numéro de suivi (selon la demande de l'utilisateur)
                if (string.IsNullOrEmpty(order.TrackingNumber))
                {
                    return BadRequest(new { message = "La commande n'a pas encore de numéro de suivi. Veuillez d'abord ajouter le numéro de suivi." });
                }

                // Générer le PDF
                var pdfBytes = _deliveryNoteService.GenerateDeliveryNotePdf(order);

                // Retourner le PDF avec le bon nom de fichier
                var fileName = $"BL_{order.OrderNumber}_{DateTime.UtcNow:yyyyMMdd}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la génération du bon de livraison pour la commande {id}");
                return StatusCode(500, new { message = "Erreur lors de la génération du bon de livraison" });
            }
        }
    }
}
