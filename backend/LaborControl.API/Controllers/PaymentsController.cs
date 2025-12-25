using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.Services;
using Stripe;
using Stripe.Checkout;

namespace LaborControl.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentsController> _logger;
    private readonly IInvoiceService _invoiceService;
    private readonly IEmailService _emailService;

    public PaymentsController(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<PaymentsController> logger,
        IInvoiceService invoiceService,
        IEmailService emailService)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _invoiceService = invoiceService;
        _emailService = emailService;

        // Configurer la clé API Stripe
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    /// <summary>
    /// Crée une session Stripe Checkout pour le paiement d'une commande
    /// </summary>
    [HttpPost("create-checkout-session")]
    public async Task<ActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
    {
        try
        {
            // Récupérer le CustomerId du token JWT
            var customerIdClaim = User.FindFirst("CustomerId");
            if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return BadRequest(new { message = "CustomerId introuvable dans le token" });
            }

            // Récupérer la commande
            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.CustomerId == customerId);

            if (order == null)
            {
                return NotFound(new { message = "Commande non trouvée" });
            }

            // Vérifier que la commande n'est pas déjà payée
            if (order.Status != "PENDING")
            {
                return BadRequest(new { message = "Cette commande a déjà été traitée" });
            }

            // Créer les options de la session Stripe Checkout
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "eur",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "10 puces NFC NTAG213 - Labor Control",
                                Description = $"Livraison standard - Commande #{order.OrderNumber}"
                            },
                            UnitAmount = (long)(order.TotalAmount * 100) // Montant en centimes
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = request.SuccessUrl + $"?session_id={{CHECKOUT_SESSION_ID}}&order_id={order.Id}",
                CancelUrl = request.CancelUrl,
                CustomerEmail = order.Customer.ContactEmail,
                Metadata = new Dictionary<string, string>
                {
                    { "order_id", order.Id.ToString() },
                    { "customer_id", customerId.ToString() },
                    { "order_number", order.OrderNumber }
                }
            };

            // Créer la session Stripe
            var service = new SessionService();
            var session = await service.CreateAsync(options);

            // Enregistrer le session ID dans la commande
            order.StripeCheckoutSessionId = session.Id;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Session Stripe créée: {session.Id} pour la commande {order.OrderNumber}");

            return Ok(new { checkoutUrl = session.Url });
        }
        catch (StripeException stripeEx)
        {
            _logger.LogError($"Erreur Stripe: {stripeEx.Message}");
            return StatusCode(500, new { message = $"Erreur Stripe: {stripeEx.Message}" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur lors de la création de la session Stripe: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la création de la session de paiement" });
        }
    }

    /// <summary>
    /// Crée une session Stripe Checkout à partir du panier
    /// </summary>
    [HttpPost("create-checkout-from-cart")]
    public async Task<ActionResult> CreateCheckoutFromCart([FromBody] CreateCheckoutFromCartRequest request)
    {
        try
        {
            // Récupérer le CustomerId du token JWT
            var customerIdClaim = User.FindFirst("CustomerId");
            if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return BadRequest(new { message = "CustomerId introuvable dans le token" });
            }

            // Récupérer le panier du client
            var cartItems = await _context.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.CustomerId == customerId)
                .ToListAsync();

            if (cartItems.Count == 0)
            {
                return BadRequest(new { message = "Votre panier est vide" });
            }

            // Récupérer les informations du client
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            // Créer les line items Stripe à partir du panier
            var lineItems = new List<SessionLineItemOptions>();
            decimal subtotalHT = 0;
            int totalChips = 0;
            const decimal TVA_RATE = 0.20m; // TVA de 20%

            foreach (var item in cartItems)
            {
                if (item.Product == null) continue;

                // Calculer le prix TTC (prix HT + 20% de TVA)
                decimal unitPriceTTC = item.UnitPrice * (1 + TVA_RATE);

                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "eur",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Name,
                            Description = item.Product.Description
                        },
                        UnitAmount = (long)(unitPriceTTC * 100) // Montant TTC en centimes
                    },
                    Quantity = item.Quantity
                });

                subtotalHT += item.UnitPrice * item.Quantity;

                // Compter le nombre total de puces (pour les NFC chips)
                if (item.Product.ProductType == "nfc_chip")
                {
                    totalChips += item.Quantity;
                }
            }

            // Calculer le sous-total TTC
            decimal subtotalTTC = subtotalHT * (1 + TVA_RATE);

            // Ajouter les frais de livraison (si applicable)
            var shippingCost = cartItems
                .Select(ci => ci.Product?.ShippingCost ?? 0)
                .Where(cost => cost > 0)
                .Distinct()
                .Sum();

            if (shippingCost > 0)
            {
                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "eur",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Frais de livraison",
                            Description = "Livraison standard"
                        },
                        UnitAmount = (long)(shippingCost * 100)
                    },
                    Quantity = 1
                });
            }

            // Créer une commande temporaire pour tracker le paiement
            var order = new Models.Order
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                OrderNumber = $"LC-{DateTime.UtcNow:yyyy}-{new Random().Next(10000000, 99999999)}",
                ChipsQuantity = totalChips,
                TotalAmount = subtotalTTC + shippingCost, // Montant TTC + frais de livraison
                Status = "PENDING",
                DeliveryAddress = request.DeliveryAddress ?? customer.Address ?? "",
                DeliveryCity = request.DeliveryCity,
                DeliveryPostalCode = request.DeliveryPostalCode,
                DeliveryCountry = request.DeliveryCountry ?? "France",
                Service = request.Service,
                Notes = request.Notes,
                ProductType = "nfc_chip", // Type principal pour les puces supplémentaires
                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Créer la session Stripe
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                Mode = "payment",
                SuccessUrl = request.SuccessUrl + $"?session_id={{CHECKOUT_SESSION_ID}}&order_id={order.Id}",
                CancelUrl = request.CancelUrl,
                CustomerEmail = customer.ContactEmail,
                Metadata = new Dictionary<string, string>
                {
                    { "order_id", order.Id.ToString() },
                    { "customer_id", customerId.ToString() },
                    { "order_number", order.OrderNumber },
                    { "source", "cart" }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            // Enregistrer le session ID dans la commande
            order.StripeCheckoutSessionId = session.Id;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Session Stripe créée depuis panier: {session.Id} pour la commande {order.OrderNumber}");

            return Ok(new { checkoutUrl = session.Url, orderId = order.Id });
        }
        catch (StripeException stripeEx)
        {
            _logger.LogError($"Erreur Stripe: {stripeEx.Message}");
            return StatusCode(500, new { message = $"Erreur Stripe: {stripeEx.Message}" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur lors de la création de la session Stripe depuis panier: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la création de la session de paiement" });
        }
    }

    /// <summary>
    /// Crée une session Stripe Checkout pour un abonnement
    /// </summary>
    [HttpPost("create-subscription-checkout")]
    public async Task<ActionResult> CreateSubscriptionCheckout([FromBody] CreateSubscriptionCheckoutRequest request)
    {
        try
        {
            // Récupérer le CustomerId du token JWT
            var customerIdClaim = User.FindFirst("CustomerId");
            if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return BadRequest(new { message = "CustomerId introuvable dans le token" });
            }

            // Récupérer les informations du client
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            // Déterminer le prix en fonction du plan et de la période de facturation
            string priceId;
            string planName;
            decimal monthlyPrice;

            if (request.PlanType.ToLower() == "starter")
            {
                planName = "Labor Control - Starter";
                monthlyPrice = 49.00m;
                // En mode test, on crée les prix dynamiquement
                // En production, vous utiliserez des Price IDs fixes créés dans le Dashboard Stripe
                priceId = request.BillingPeriod == "annual"
                    ? "price_starter_annual" // À créer dans Stripe Dashboard
                    : "price_starter_monthly"; // À créer dans Stripe Dashboard
            }
            else if (request.PlanType.ToLower() == "medium")
            {
                planName = "Labor Control - Medium";
                monthlyPrice = 99.00m;
                priceId = request.BillingPeriod == "annual"
                    ? "price_medium_annual"
                    : "price_medium_monthly";
            }
            else
            {
                return BadRequest(new { message = "Type de plan non valide" });
            }

            // Calculer le montant avec remise de 15% pour l'annuel
            decimal amount = request.BillingPeriod == "annual"
                ? monthlyPrice * 12 * 0.85m
                : monthlyPrice;

            // Créer la session Stripe avec des prix dynamiques (pour le mode test)
            var lineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "eur",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = planName,
                            Description = request.BillingPeriod == "annual"
                                ? "Facturation annuelle (15% de remise)"
                                : "Facturation mensuelle"
                        },
                        Recurring = new SessionLineItemPriceDataRecurringOptions
                        {
                            Interval = request.BillingPeriod == "annual" ? "year" : "month"
                        },
                        UnitAmount = (long)(amount * 100) // Montant en centimes
                    },
                    Quantity = 1
                }
            };

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                Mode = "subscription",
                SuccessUrl = $"{_configuration["Frontend:Url"]}/subscription-success?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{_configuration["Frontend:Url"]}/subscription-plans",
                CustomerEmail = customer.ContactEmail,
                Metadata = new Dictionary<string, string>
                {
                    { "customer_id", customerId.ToString() },
                    { "plan_type", request.PlanType },
                    { "billing_period", request.BillingPeriod }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation($"Session Stripe créée pour abonnement {request.PlanType} ({request.BillingPeriod}): {session.Id}");

            return Ok(new { checkoutUrl = session.Url });
        }
        catch (StripeException stripeEx)
        {
            _logger.LogError($"Erreur Stripe lors de la création de l'abonnement: {stripeEx.Message}");
            return StatusCode(500, new { message = $"Erreur Stripe: {stripeEx.Message}" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur lors de la création de la session d'abonnement: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la création de la session d'abonnement" });
        }
    }

    /// <summary>
    /// Webhook Stripe pour recevoir les événements de paiement
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _configuration["Stripe:WebhookSecret"]
            );

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;
                if (session != null && session.Metadata.ContainsKey("order_id"))
                {
                    var orderId = Guid.Parse(session.Metadata["order_id"]);

                    var order = await _context.Orders
                        .Include(o => o.Customer)
                        .FirstOrDefaultAsync(o => o.Id == orderId);

                    if (order != null)
                    {
                        order.Status = "PAID";
                        order.StripePaymentIntentId = session.PaymentIntentId;
                        order.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();

                        _logger.LogInformation($"Paiement confirmé pour la commande {order.OrderNumber}");

                        // Générer la facture automatiquement après paiement
                        try
                        {
                            _logger.LogInformation($"Génération automatique de la facture pour {order.OrderNumber}");

                            // S'assurer que le dossier invoices existe
                            var webRootPath = _configuration["WebRootPath"] ?? "wwwroot";
                            var invoicesDir = Path.Combine(webRootPath, "invoices");

                            if (!Directory.Exists(invoicesDir))
                            {
                                Directory.CreateDirectory(invoicesDir);
                                _logger.LogInformation($"Dossier invoices créé : {invoicesDir}");
                            }

                            var invoicePath = await _invoiceService.GenerateInvoicePdfAsync(order);
                            order.InvoicePdfPath = invoicePath;
                            order.InvoiceGeneratedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();

                            _logger.LogInformation($"Facture générée avec succès : {invoicePath}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Erreur lors de la génération de la facture pour {order.OrderNumber}");
                            // Ne pas bloquer le webhook si la génération échoue
                        }

                        // Envoyer l'email de confirmation de paiement avec le lien vers la facture
                        try
                        {
                            if (!string.IsNullOrEmpty(order.Customer.ContactEmail))
                            {
                                var emailSent = await _emailService.SendPaymentConfirmationEmailAsync(
                                    order.Customer.ContactEmail,
                                    order.Customer.Name,
                                    order.OrderNumber,
                                    order.Id,
                                    order.TotalAmount,
                                    order.ChipsQuantity
                                );

                                if (emailSent)
                                {
                                    _logger.LogInformation($"[EMAIL] Email de confirmation de paiement envoyé à {order.Customer.ContactEmail} pour la commande {order.OrderNumber}");
                                }
                                else
                                {
                                    _logger.LogWarning($"[EMAIL] Échec de l'envoi de l'email de confirmation de paiement à {order.Customer.ContactEmail}");
                                }
                            }
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, $"[EMAIL] Erreur lors de l'envoi de l'email de confirmation de paiement pour {order.OrderNumber}");
                            // Ne pas bloquer le webhook si l'email échoue
                        }

                        // Si le paiement vient d'un panier, vider le panier
                        if (session.Metadata.ContainsKey("source") && session.Metadata["source"] == "cart")
                        {
                            var customerId = Guid.Parse(session.Metadata["customer_id"]);
                            var cartItems = await _context.CartItems
                                .Where(ci => ci.CustomerId == customerId)
                                .ToListAsync();

                            _context.CartItems.RemoveRange(cartItems);
                            await _context.SaveChangesAsync();

                            _logger.LogInformation($"Panier vidé pour le client {customerId} après paiement");
                        }
                    }
                }
            }
            // Gérer les événements d'abonnement
            else if (stripeEvent.Type == "customer.subscription.created" ||
                     stripeEvent.Type == "customer.subscription.updated")
            {
                var subscription = stripeEvent.Data.Object as Stripe.Subscription;
                if (subscription != null)
                {
                    _logger.LogInformation($"Abonnement créé/mis à jour: {subscription.Id} - Status: {subscription.Status}");
                    // TODO: Enregistrer l'abonnement dans la base de données
                }
            }
            else if (stripeEvent.Type == "customer.subscription.deleted")
            {
                var subscription = stripeEvent.Data.Object as Stripe.Subscription;
                if (subscription != null)
                {
                    _logger.LogInformation($"Abonnement annulé: {subscription.Id}");
                    // TODO: Marquer l'abonnement comme annulé dans la base de données
                }
            }

            return Ok();
        }
        catch (StripeException e)
        {
            _logger.LogError($"Erreur webhook Stripe: {e.Message}");
            return BadRequest();
        }
    }

    /// <summary>
    /// Diagnostic de commande (temporaire)
    /// </summary>
    [HttpGet("invoice/{orderId}/diagnostic")]
    public async Task<IActionResult> DiagnosticInvoice(Guid orderId)
    {
        try
        {
            var customerIdClaim = User.FindFirst("CustomerId");
            if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return Ok(new { error = "CustomerId introuvable dans le token" });
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customerId);

            if (order == null)
            {
                return Ok(new {
                    error = "Commande non trouvée",
                    orderId = orderId,
                    customerId = customerId
                });
            }

            return Ok(new
            {
                success = true,
                orderNumber = order.OrderNumber,
                status = order.Status,
                customerId = order.CustomerId,
                customerLoaded = order.Customer != null,
                customerName = order.Customer == null ? null : order.Customer.Name,
                customerContactName = order.Customer == null ? null : order.Customer.ContactName,
                customerEmail = order.Customer == null ? null : order.Customer.ContactEmail,
                deliveryAddress = order.DeliveryAddress,
                invoicePath = order.InvoicePdfPath,
                totalAmount = order.TotalAmount
            });
        }
        catch (Exception ex)
        {
            return Ok(new {
                error = ex.Message,
                stackTrace = ex.StackTrace,
                innerException = ex.InnerException == null ? null : ex.InnerException.Message
            });
        }
    }

    /// <summary>
    /// Télécharge la facture PDF d'une commande
    /// </summary>
    [HttpGet("invoice/{orderId}")]
    public async Task<IActionResult> DownloadInvoice(Guid orderId)
    {
        try
        {
            // Récupérer le CustomerId du token JWT
            var customerIdClaim = User.FindFirst("CustomerId");
            if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                _logger.LogWarning("CustomerId introuvable dans le token");
                return BadRequest(new { message = "CustomerId introuvable dans le token" });
            }

            // Récupérer la commande avec les infos du client
            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customerId);

            if (order == null)
            {
                _logger.LogWarning($"Commande non trouvée : {orderId} pour le client {customerId}");
                return NotFound(new { message = "Commande non trouvée" });
            }

            // Log détaillé de la commande trouvée
            _logger.LogInformation($"Commande trouvée: {order.OrderNumber}, Status: {order.Status}, CustomerId: {order.CustomerId}, Customer chargé: {order.Customer != null}");

            // Vérification critique : les informations client sont REQUISES pour générer une facture
            if (order.Customer == null)
            {
                _logger.LogError($"ERREUR CRITIQUE: Informations client non chargées pour la commande {order.OrderNumber} (OrderId: {orderId}, CustomerId: {order.CustomerId})");
                return StatusCode(500, new { message = "Informations client manquantes. Veuillez contacter le support." });
            }

            _logger.LogInformation($"Client chargé: {order.Customer.Name ?? order.Customer.ContactName}, Email: {order.Customer.ContactEmail}");

            // IMPORTANT : Autoriser l'accès aux factures pour toutes les commandes sauf CANCELLED
            // Cela permet de générer des factures même pour les commandes en attente de paiement (PENDING)
            if (order.Status == "CANCELLED")
            {
                _logger.LogWarning($"Tentative de téléchargement de facture pour une commande annulée: {order.OrderNumber}");
                return BadRequest(new { message = "Impossible de générer une facture pour une commande annulée" });
            }

            // Si la facture n'existe pas encore, la générer à la volée
            if (string.IsNullOrEmpty(order.InvoicePdfPath))
            {
                _logger.LogInformation($"Génération de la facture à la volée pour la commande {order.OrderNumber} (statut: {order.Status}, client: {order.Customer.Name ?? order.Customer.ContactName})");

                try
                {
                    // S'assurer que le dossier invoices existe
                    var webRootPath = _configuration["WebRootPath"] ?? "wwwroot";
                    var invoicesDir = Path.Combine(webRootPath, "invoices");

                    _logger.LogInformation($"Vérification du dossier invoices: {invoicesDir}");

                    if (!Directory.Exists(invoicesDir))
                    {
                        Directory.CreateDirectory(invoicesDir);
                        _logger.LogInformation($"Dossier invoices créé : {invoicesDir}");
                    }
                    else
                    {
                        _logger.LogInformation($"Dossier invoices existe déjà : {invoicesDir}");
                    }

                    // Générer la facture
                    _logger.LogInformation($"Appel du service de génération de facture...");
                    var invoicePath = await _invoiceService.GenerateInvoicePdfAsync(order);

                    // Enregistrer le chemin dans la base de données
                    order.InvoicePdfPath = invoicePath;
                    order.InvoiceGeneratedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Facture générée avec succès et enregistrée : {invoicePath}");
                }
                catch (InvalidOperationException invalidEx)
                {
                    _logger.LogError(invalidEx, $"Opération invalide lors de la génération de la facture pour la commande {order.OrderNumber}");
                    return StatusCode(500, new { message = $"Données manquantes: {invalidEx.Message}" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Erreur lors de la génération de la facture pour la commande {order.OrderNumber}");
                    return StatusCode(500, new { message = $"Erreur lors de la génération de la facture: {ex.Message}. Veuillez contacter le support si le problème persiste." });
                }
            }
            else
            {
                _logger.LogInformation($"Facture déjà existante pour la commande {order.OrderNumber}: {order.InvoicePdfPath}");
            }

            // Construire le chemin complet du fichier
            var webRoot = _configuration["WebRootPath"] ?? "wwwroot";
            var filePath = Path.Combine(webRoot, order.InvoicePdfPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            _logger.LogInformation($"Tentative de lecture du fichier : {filePath}");

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogError($"Fichier de facture introuvable : {filePath}");
                return NotFound(new { message = "Fichier de facture introuvable" });
            }

            // Lire le fichier
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var fileName = $"Facture_{order.OrderNumber}.pdf";

            _logger.LogInformation($"Facture téléchargée avec succès : {fileName}");

            return File(fileBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erreur lors du téléchargement de la facture pour la commande {orderId}");
            return StatusCode(500, new { message = $"Erreur lors du téléchargement de la facture: {ex.Message}" });
        }
    }
}

public class CreateCheckoutSessionRequest
{
    public Guid OrderId { get; set; }
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}

public class CreateCheckoutFromCartRequest
{
    public string? DeliveryAddress { get; set; }
    public string? DeliveryCity { get; set; }
    public string? DeliveryPostalCode { get; set; }
    public string? DeliveryCountry { get; set; }
    public string? Service { get; set; }
    public string? Notes { get; set; }
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}

public class CreateSubscriptionCheckoutRequest
{
    public string PlanType { get; set; } = string.Empty; // "starter" ou "medium"
    public string BillingPeriod { get; set; } = string.Empty; // "monthly" ou "annual"
}
