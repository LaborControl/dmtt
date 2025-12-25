using LaborControl.API.Data;
using LaborControl.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaborControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ShipmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IBoxtalService _boxtalService;
        private readonly ILogger<ShipmentsController> _logger;

        public ShipmentsController(
            ApplicationDbContext context,
            IBoxtalService boxtalService,
            ILogger<ShipmentsController> logger)
        {
            _context = context;
            _boxtalService = boxtalService;
            _logger = logger;
        }

        /// <summary>
        /// Obtient un devis d'expédition pour une commande
        /// </summary>
        [HttpGet("quote/{orderId}")]
        public async Task<ActionResult> GetShippingQuote(Guid orderId, [FromQuery] int? weightInGrams)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    return NotFound(new { message = "Commande introuvable" });
                }

                // Vérifier que la commande est prête à être expédiée
                if (order.Status != "READY_TO_SHIP" && order.Status != "PAID")
                {
                    return BadRequest(new { message = $"La commande n'est pas prête à être expédiée (statut: {order.Status})" });
                }

                var weight = weightInGrams ?? 500; // Par défaut 500g pour 10 puces
                var quote = await _boxtalService.GetShippingQuoteAsync(order, weight);

                if (!quote.Success)
                {
                    return BadRequest(new { message = quote.ErrorMessage });
                }

                return Ok(new
                {
                    orderId = order.Id,
                    orderNumber = order.OrderNumber,
                    weightInGrams = weight,
                    offers = quote.Offers.Select(o => new
                    {
                        code = o.Code,
                        carrierName = o.CarrierName,
                        serviceName = o.ServiceName,
                        priceExclTax = o.PriceExclTax,
                        priceInclTax = o.PriceInclTax,
                        deliveryDelayInDays = o.DeliveryDelayInDays,
                        estimatedDeliveryDate = o.EstimatedDeliveryDate,
                        description = o.Description,
                        isPickupPoint = o.IsPickupPoint
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting shipping quote for order {orderId}");
                return StatusCode(500, new { message = "Erreur lors de la récupération du devis" });
            }
        }

        /// <summary>
        /// Crée une expédition Boxtal pour une commande
        /// </summary>
        [HttpPost("create")]
        public async Task<ActionResult> CreateShipment([FromBody] CreateShipmentRequest request)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .FirstOrDefaultAsync(o => o.Id == request.OrderId);

                if (order == null)
                {
                    return NotFound(new { message = "Commande introuvable" });
                }

                // Vérifier que la commande est prête
                if (order.Status != "READY_TO_SHIP" && order.Status != "PAID")
                {
                    return BadRequest(new { message = $"La commande n'est pas prête à être expédiée (statut: {order.Status})" });
                }

                // Vérifier qu'il n'existe pas déjà une expédition active
                var existingShipment = await _context.BoxtalShipments
                    .Where(s => s.OrderId == request.OrderId && s.Status != "CANCELLED" && s.Status != "ERROR")
                    .FirstOrDefaultAsync();

                if (existingShipment != null)
                {
                    return BadRequest(new { message = "Une expédition existe déjà pour cette commande" });
                }

                var shipment = await _boxtalService.CreateShipmentAsync(
                    order,
                    request.SelectedOffer,
                    request.WeightInGrams ?? 500
                );

                return Ok(new
                {
                    shipmentId = shipment.Id,
                    boxtalReference = shipment.BoxtalReference,
                    trackingNumber = shipment.TrackingNumber,
                    trackingUrl = shipment.TrackingUrl,
                    labelUrl = shipment.LabelUrl,
                    carrierCode = shipment.CarrierCode,
                    serviceName = shipment.ServiceName,
                    priceInclTax = shipment.PriceInclTax,
                    estimatedDeliveryDate = shipment.EstimatedDeliveryDate,
                    status = shipment.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating shipment for order {request.OrderId}");
                return StatusCode(500, new { message = $"Erreur lors de la création de l'expédition: {ex.Message}" });
            }
        }

        /// <summary>
        /// Récupère toutes les expéditions
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> GetAllShipments()
        {
            try
            {
                var shipments = await _context.BoxtalShipments
                    .Include(s => s.Order)
                        .ThenInclude(o => o!.Customer)
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new
                    {
                        id = s.Id,
                        orderId = s.OrderId,
                        orderNumber = s.Order!.OrderNumber,
                        customerName = s.Order.Customer!.Name,
                        boxtalReference = s.BoxtalReference,
                        trackingNumber = s.TrackingNumber,
                        trackingUrl = s.TrackingUrl,
                        carrierCode = s.CarrierCode,
                        serviceName = s.ServiceName,
                        priceInclTax = s.PriceInclTax,
                        status = s.Status,
                        createdAt = s.CreatedAt,
                        shippedAt = s.ShippedAt,
                        estimatedDeliveryDate = s.EstimatedDeliveryDate,
                        deliveredAt = s.DeliveredAt
                    })
                    .ToListAsync();

                return Ok(shipments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching shipments");
                return StatusCode(500, new { message = "Erreur lors de la récupération des expéditions" });
            }
        }

        /// <summary>
        /// Récupère une expédition par ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult> GetShipment(Guid id)
        {
            try
            {
                var shipment = await _context.BoxtalShipments
                    .Include(s => s.Order)
                        .ThenInclude(o => o!.Customer)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (shipment == null)
                {
                    return NotFound(new { message = "Expédition introuvable" });
                }

                return Ok(new
                {
                    id = shipment.Id,
                    orderId = shipment.OrderId,
                    orderNumber = shipment.Order?.OrderNumber,
                    customerName = shipment.Order?.Customer?.Name,
                    boxtalReference = shipment.BoxtalReference,
                    selectedOffer = shipment.SelectedOffer,
                    trackingNumber = shipment.TrackingNumber,
                    trackingUrl = shipment.TrackingUrl,
                    labelUrl = shipment.LabelUrl,
                    carrierCode = shipment.CarrierCode,
                    serviceName = shipment.ServiceName,
                    priceExclTax = shipment.PriceExclTax,
                    priceInclTax = shipment.PriceInclTax,
                    weightInGrams = shipment.WeightInGrams,
                    status = shipment.Status,
                    errorMessage = shipment.ErrorMessage,
                    createdAt = shipment.CreatedAt,
                    updatedAt = shipment.UpdatedAt,
                    shippedAt = shipment.ShippedAt,
                    estimatedDeliveryDate = shipment.EstimatedDeliveryDate,
                    deliveredAt = shipment.DeliveredAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching shipment {id}");
                return StatusCode(500, new { message = "Erreur lors de la récupération de l'expédition" });
            }
        }

        /// <summary>
        /// Télécharge l'étiquette d'expédition
        /// </summary>
        [HttpGet("{id}/label")]
        public async Task<ActionResult> GetShippingLabel(Guid id)
        {
            try
            {
                var labelUrl = await _boxtalService.GetShippingLabelAsync(id);
                return Ok(new { labelUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting shipping label for shipment {id}");
                return StatusCode(500, new { message = $"Erreur: {ex.Message}" });
            }
        }

        /// <summary>
        /// Met à jour le statut de suivi d'une expédition
        /// </summary>
        [HttpPost("{id}/update-tracking")]
        public async Task<ActionResult> UpdateTracking(Guid id)
        {
            try
            {
                var shipment = await _boxtalService.UpdateTrackingStatusAsync(id);

                return Ok(new
                {
                    id = shipment.Id,
                    status = shipment.Status,
                    shippedAt = shipment.ShippedAt,
                    deliveredAt = shipment.DeliveredAt,
                    updatedAt = shipment.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating tracking for shipment {id}");
                return StatusCode(500, new { message = $"Erreur: {ex.Message}" });
            }
        }

        /// <summary>
        /// Annule une expédition
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> CancelShipment(Guid id)
        {
            try
            {
                var success = await _boxtalService.CancelShipmentAsync(id);

                if (!success)
                {
                    return BadRequest(new { message = "Impossible d'annuler cette expédition" });
                }

                return Ok(new { message = "Expédition annulée avec succès" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling shipment {id}");
                return StatusCode(500, new { message = $"Erreur: {ex.Message}" });
            }
        }

        /// <summary>
        /// Recherche des points relais
        /// </summary>
        [HttpGet("pickup-points")]
        public async Task<ActionResult> FindPickupPoints(
            [FromQuery] string postalCode,
            [FromQuery] string city,
            [FromQuery] string country = "FR")
        {
            try
            {
                if (string.IsNullOrEmpty(postalCode) || string.IsNullOrEmpty(city))
                {
                    return BadRequest(new { message = "Code postal et ville requis" });
                }

                var pickupPoints = await _boxtalService.FindPickupPointsAsync(postalCode, city, country);

                return Ok(new
                {
                    pickupPoints = pickupPoints.Select(p => new
                    {
                        code = p.Code,
                        name = p.Name,
                        address = p.Address,
                        postalCode = p.PostalCode,
                        city = p.City,
                        country = p.Country,
                        latitude = p.Latitude,
                        longitude = p.Longitude,
                        distanceInKm = p.DistanceInKm,
                        openingHours = p.OpeningHours
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding pickup points");
                return StatusCode(500, new { message = "Erreur lors de la recherche de points relais" });
            }
        }
    }

    public class CreateShipmentRequest
    {
        public Guid OrderId { get; set; }
        public string SelectedOffer { get; set; } = string.Empty;
        public int? WeightInGrams { get; set; }
    }
}
