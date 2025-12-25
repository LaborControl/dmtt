using LaborControl.API.Data;
using LaborControl.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LaborControl.API.Services
{
    /// <summary>
    /// Service d'intégration avec l'API Boxtal pour l'expédition de commandes
    /// Documentation: https://api.boxtal.com/v1/documentation
    /// </summary>
    public class BoxtalService : IBoxtalService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BoxtalService> _logger;

        private const string BOXTAL_API_BASE_URL = "https://api.boxtal.com/v1";
        private const string BOXTAL_SANDBOX_URL = "https://api-sandbox.boxtal.com/v1";

        public BoxtalService(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<BoxtalService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        private string GetBaseUrl()
        {
            var useSandbox = _configuration.GetValue<bool>("Boxtal:UseSandbox", true);
            return useSandbox ? BOXTAL_SANDBOX_URL : BOXTAL_API_BASE_URL;
        }

        private HttpClient GetHttpClient()
        {
            var client = _httpClientFactory.CreateClient();
            var apiKey = _configuration["Boxtal:ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Boxtal API Key not configured");
            }

            // Authentification par API Key dans le header Authorization
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }

        public async Task<BoxtalQuoteResponse> GetShippingQuoteAsync(Order order, int weightInGrams = 500)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(order.CustomerId);
                if (customer == null)
                {
                    return new BoxtalQuoteResponse
                    {
                        Success = false,
                        ErrorMessage = "Client introuvable"
                    };
                }

                var requestBody = new
                {
                    expediteur = new
                    {
                        pays = "FR",
                        code_postal = "75001", // Adresse de l'expéditeur (Labor Control)
                        ville = "Paris",
                        type = "entreprise"
                    },
                    destinataire = new
                    {
                        pays = order.DeliveryCountry ?? "FR",
                        code_postal = order.DeliveryPostalCode,
                        ville = order.DeliveryCity,
                        type = "entreprise"
                    },
                    colis = new[]
                    {
                        new
                        {
                            poids = (decimal)weightInGrams / 1000, // Convertir en kg
                            longueur = 20, // cm
                            largeur = 15,  // cm
                            hauteur = 5    // cm
                        }
                    },
                    valeur = order.TotalAmount
                };

                var client = GetHttpClient();
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{GetBaseUrl()}/quote", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Boxtal quote request failed: {response.StatusCode} - {responseBody}");
                    return new BoxtalQuoteResponse
                    {
                        Success = false,
                        ErrorMessage = $"Erreur API Boxtal: {response.StatusCode}"
                    };
                }

                // Parser la réponse Boxtal
                var boxtalResponse = JsonSerializer.Deserialize<BoxtalQuoteApiResponse>(responseBody);

                if (boxtalResponse?.offers == null || !boxtalResponse.offers.Any())
                {
                    return new BoxtalQuoteResponse
                    {
                        Success = false,
                        ErrorMessage = "Aucune offre d'expédition disponible"
                    };
                }

                var offers = boxtalResponse.offers.Select(o => new BoxtalOffer
                {
                    Code = o.code ?? "",
                    CarrierName = o.carrier?.name ?? "",
                    ServiceName = o.service?.name ?? "",
                    PriceExclTax = o.price?.ht ?? 0,
                    PriceInclTax = o.price?.ttc ?? 0,
                    DeliveryDelayInDays = o.delivery_delay ?? 0,
                    EstimatedDeliveryDate = o.estimated_delivery_date,
                    Description = o.description,
                    IsPickupPoint = o.pickup_point ?? false
                }).ToList();

                return new BoxtalQuoteResponse
                {
                    Success = true,
                    Offers = offers
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting Boxtal quote for order {order.Id}");
                return new BoxtalQuoteResponse
                {
                    Success = false,
                    ErrorMessage = $"Erreur: {ex.Message}"
                };
            }
        }

        public async Task<BoxtalShipment> CreateShipmentAsync(Order order, string selectedOffer, int weightInGrams = 500)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(order.CustomerId);
                if (customer == null)
                {
                    throw new InvalidOperationException("Client introuvable");
                }

                // Créer la demande d'expédition Boxtal
                var requestBody = new
                {
                    offer = selectedOffer,
                    expediteur = new
                    {
                        entreprise = "Labor Control",
                        civilite = "M",
                        prenom = "Support",
                        nom = "Labor Control",
                        adresse = "1 Rue de l'Innovation",
                        code_postal = "75001",
                        ville = "Paris",
                        pays = "FR",
                        email = "contact@labor-control.fr",
                        telephone = "+33123456789"
                    },
                    destinataire = new
                    {
                        entreprise = customer.Name,
                        civilite = "M",
                        prenom = customer.ContactName?.Split(' ').FirstOrDefault() ?? "",
                        nom = customer.ContactName?.Split(' ').LastOrDefault() ?? customer.ContactName,
                        adresse = order.DeliveryAddress,
                        code_postal = order.DeliveryPostalCode,
                        ville = order.DeliveryCity,
                        pays = order.DeliveryCountry ?? "FR",
                        email = customer.ContactEmail,
                        telephone = customer.ContactPhone ?? ""
                    },
                    colis = new[]
                    {
                        new
                        {
                            poids = (decimal)weightInGrams / 1000,
                            longueur = 20,
                            largeur = 15,
                            hauteur = 5,
                            description = $"Puces RFID - Commande {order.OrderNumber}",
                            reference = order.PackagingCode
                        }
                    },
                    valeur = order.TotalAmount,
                    reference_commande = order.OrderNumber,
                    description = $"{order.ChipsQuantity} puces RFID Labor Control"
                };

                var client = GetHttpClient();
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{GetBaseUrl()}/shipments", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Boxtal shipment creation failed: {response.StatusCode} - {responseBody}");
                    throw new Exception($"Erreur création expédition Boxtal: {response.StatusCode}");
                }

                var boxtalResponse = JsonSerializer.Deserialize<BoxtalShipmentApiResponse>(responseBody);

                if (boxtalResponse == null)
                {
                    throw new Exception("Réponse Boxtal invalide");
                }

                // Créer l'enregistrement BoxtalShipment
                var shipment = new BoxtalShipment
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    BoxtalReference = boxtalResponse.reference,
                    SelectedOffer = selectedOffer,
                    CarrierCode = boxtalResponse.carrier?.code,
                    ServiceName = boxtalResponse.service?.name,
                    TrackingNumber = boxtalResponse.tracking_number,
                    TrackingUrl = boxtalResponse.tracking_url,
                    PriceExclTax = boxtalResponse.price?.ht ?? 0,
                    PriceInclTax = boxtalResponse.price?.ttc ?? 0,
                    WeightInGrams = weightInGrams,
                    Status = "SHIPMENT_CREATED",
                    LabelUrl = boxtalResponse.label_url,
                    BoxtalResponse = responseBody,
                    EstimatedDeliveryDate = boxtalResponse.estimated_delivery_date,
                    CreatedAt = DateTime.UtcNow
                };

                _context.BoxtalShipments.Add(shipment);

                // Mettre à jour la commande
                order.CarrierName = boxtalResponse.carrier?.name;
                order.TrackingNumber = boxtalResponse.tracking_number;
                order.Status = "SHIPPED";
                order.ShippedAt = DateTime.UtcNow;
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Boxtal shipment created for order {order.OrderNumber}: {shipment.BoxtalReference}");

                return shipment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating Boxtal shipment for order {order.Id}");

                // Sauvegarder l'erreur dans la base
                var errorShipment = new BoxtalShipment
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    SelectedOffer = selectedOffer,
                    WeightInGrams = weightInGrams,
                    Status = "ERROR",
                    ErrorMessage = ex.Message,
                    CreatedAt = DateTime.UtcNow
                };

                _context.BoxtalShipments.Add(errorShipment);
                await _context.SaveChangesAsync();

                throw;
            }
        }

        public async Task<string> GetShippingLabelAsync(Guid shipmentId)
        {
            var shipment = await _context.BoxtalShipments.FindAsync(shipmentId);

            if (shipment == null)
            {
                throw new InvalidOperationException("Expédition introuvable");
            }

            if (!string.IsNullOrEmpty(shipment.LabelUrl))
            {
                return shipment.LabelUrl;
            }

            if (string.IsNullOrEmpty(shipment.BoxtalReference))
            {
                throw new InvalidOperationException("Référence Boxtal manquante");
            }

            // Récupérer l'étiquette depuis l'API Boxtal
            var client = GetHttpClient();
            var response = await client.GetAsync($"{GetBaseUrl()}/shipments/{shipment.BoxtalReference}/label");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Erreur récupération étiquette: {response.StatusCode}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var labelResponse = JsonSerializer.Deserialize<BoxtalLabelResponse>(responseBody);

            if (labelResponse?.label_url != null)
            {
                shipment.LabelUrl = labelResponse.label_url;
                shipment.Status = "LABEL_GENERATED";
                shipment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return labelResponse.label_url;
            }

            throw new Exception("URL d'étiquette non disponible");
        }

        public async Task<BoxtalShipment> UpdateTrackingStatusAsync(Guid shipmentId)
        {
            var shipment = await _context.BoxtalShipments
                .Include(s => s.Order)
                .FirstOrDefaultAsync(s => s.Id == shipmentId);

            if (shipment == null)
            {
                throw new InvalidOperationException("Expédition introuvable");
            }

            if (string.IsNullOrEmpty(shipment.BoxtalReference))
            {
                throw new InvalidOperationException("Référence Boxtal manquante");
            }

            var client = GetHttpClient();
            var response = await client.GetAsync($"{GetBaseUrl()}/tracking/{shipment.BoxtalReference}");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Erreur mise à jour tracking: {response.StatusCode}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var trackingResponse = JsonSerializer.Deserialize<BoxtalTrackingResponse>(responseBody);

            if (trackingResponse != null)
            {
                // Mettre à jour le statut selon le tracking
                if (trackingResponse.status == "delivered")
                {
                    shipment.Status = "DELIVERED";
                    shipment.DeliveredAt = trackingResponse.delivered_at ?? DateTime.UtcNow;

                    if (shipment.Order != null)
                    {
                        shipment.Order.Status = "DELIVERED";
                        shipment.Order.DeliveredAt = shipment.DeliveredAt;
                        shipment.Order.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else if (trackingResponse.status == "in_transit")
                {
                    shipment.Status = "SHIPPED";
                    shipment.ShippedAt = trackingResponse.shipped_at;
                }

                shipment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return shipment;
        }

        public async Task<bool> CancelShipmentAsync(Guid shipmentId)
        {
            try
            {
                var shipment = await _context.BoxtalShipments
                    .Include(s => s.Order)
                    .FirstOrDefaultAsync(s => s.Id == shipmentId);

                if (shipment == null)
                {
                    return false;
                }

                if (string.IsNullOrEmpty(shipment.BoxtalReference))
                {
                    return false;
                }

                var client = GetHttpClient();
                var response = await client.DeleteAsync($"{GetBaseUrl()}/shipments/{shipment.BoxtalReference}");

                if (response.IsSuccessStatusCode)
                {
                    shipment.Status = "CANCELLED";
                    shipment.UpdatedAt = DateTime.UtcNow;

                    if (shipment.Order != null)
                    {
                        shipment.Order.Status = "READY_TO_SHIP"; // Revenir à l'état précédent
                        shipment.Order.TrackingNumber = null;
                        shipment.Order.ShippedAt = null;
                        shipment.Order.UpdatedAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling Boxtal shipment {shipmentId}");
                return false;
            }
        }

        public async Task<List<BoxtalPickupPoint>> FindPickupPointsAsync(string postalCode, string city, string country = "FR")
        {
            try
            {
                var client = GetHttpClient();
                var url = $"{GetBaseUrl()}/pickup-points?postal_code={postalCode}&city={Uri.EscapeDataString(city)}&country={country}";

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Boxtal pickup points request failed: {response.StatusCode}");
                    return new List<BoxtalPickupPoint>();
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var pickupResponse = JsonSerializer.Deserialize<BoxtalPickupPointsResponse>(responseBody);

                if (pickupResponse?.pickup_points == null)
                {
                    return new List<BoxtalPickupPoint>();
                }

                return pickupResponse.pickup_points.Select(p => new BoxtalPickupPoint
                {
                    Code = p.code ?? "",
                    Name = p.name ?? "",
                    Address = p.address ?? "",
                    PostalCode = p.postal_code ?? "",
                    City = p.city ?? "",
                    Country = p.country ?? "",
                    Latitude = p.latitude,
                    Longitude = p.longitude,
                    DistanceInKm = p.distance_km,
                    OpeningHours = p.opening_hours ?? new List<string>()
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Boxtal pickup points");
                return new List<BoxtalPickupPoint>();
            }
        }

        #region API Response Models

        private class BoxtalQuoteApiResponse
        {
            public List<BoxtalOfferApi>? offers { get; set; }
        }

        private class BoxtalOfferApi
        {
            public string? code { get; set; }
            public BoxtalCarrierApi? carrier { get; set; }
            public BoxtalServiceApi? service { get; set; }
            public BoxtalPriceApi? price { get; set; }
            public int? delivery_delay { get; set; }
            public DateTime? estimated_delivery_date { get; set; }
            public string? description { get; set; }
            public bool? pickup_point { get; set; }
        }

        private class BoxtalShipmentApiResponse
        {
            public string? reference { get; set; }
            public string? tracking_number { get; set; }
            public string? tracking_url { get; set; }
            public string? label_url { get; set; }
            public BoxtalCarrierApi? carrier { get; set; }
            public BoxtalServiceApi? service { get; set; }
            public BoxtalPriceApi? price { get; set; }
            public DateTime? estimated_delivery_date { get; set; }
        }

        private class BoxtalCarrierApi
        {
            public string? code { get; set; }
            public string? name { get; set; }
        }

        private class BoxtalServiceApi
        {
            public string? code { get; set; }
            public string? name { get; set; }
        }

        private class BoxtalPriceApi
        {
            public decimal ht { get; set; }
            public decimal ttc { get; set; }
        }

        private class BoxtalLabelResponse
        {
            public string? label_url { get; set; }
        }

        private class BoxtalTrackingResponse
        {
            public string? status { get; set; }
            public DateTime? shipped_at { get; set; }
            public DateTime? delivered_at { get; set; }
        }

        private class BoxtalPickupPointsResponse
        {
            public List<BoxtalPickupPointApi>? pickup_points { get; set; }
        }

        private class BoxtalPickupPointApi
        {
            public string? code { get; set; }
            public string? name { get; set; }
            public string? address { get; set; }
            public string? postal_code { get; set; }
            public string? city { get; set; }
            public string? country { get; set; }
            public decimal latitude { get; set; }
            public decimal longitude { get; set; }
            public decimal distance_km { get; set; }
            public List<string>? opening_hours { get; set; }
        }

        #endregion
    }
}
