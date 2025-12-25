using Stripe;
using Stripe.Checkout;

namespace LaborControl.API.Services
{
    public class StripePaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<StripePaymentService> _logger;

        public StripePaymentService(IConfiguration configuration, ILogger<StripePaymentService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            // Configurer la clé API Stripe
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        public async Task<SessionCreateResponse> CreateCheckoutSessionAsync(
            string orderNumber,
            decimal amount,
            string customerEmail,
            string successUrl,
            string cancelUrl)
        {
            try
            {
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string>
                    {
                        "card",
                    },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = "eur",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = "Pack découverte LABOR CONTROL",
                                    Description = "10 puces NFC gratuites + frais de port",
                                },
                                UnitAmount = (long)(amount * 100), // Convertir en centimes
                            },
                            Quantity = 1,
                        },
                    },
                    Mode = "payment",
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl,
                    CustomerEmail = customerEmail,
                    Metadata = new Dictionary<string, string>
                    {
                        { "order_number", orderNumber }
                    }
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                return new SessionCreateResponse
                {
                    Success = true,
                    SessionId = session.Id,
                    CheckoutUrl = session.Url
                };
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Erreur Stripe lors de la création de la session de paiement");
                return new SessionCreateResponse
                {
                    Success = false,
                    ErrorMessage = $"Erreur lors de la création du paiement : {ex.Message}"
                };
            }
        }

        public async Task<PaymentVerificationResult> VerifyPaymentAsync(string sessionId)
        {
            try
            {
                var service = new SessionService();
                var session = await service.GetAsync(sessionId);

                return new PaymentVerificationResult
                {
                    Success = session.PaymentStatus == "paid",
                    PaymentIntentId = session.PaymentIntentId,
                    OrderNumber = session.Metadata.GetValueOrDefault("order_number")
                };
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Erreur Stripe lors de la vérification du paiement");
                return new PaymentVerificationResult
                {
                    Success = false,
                    ErrorMessage = $"Erreur lors de la vérification du paiement : {ex.Message}"
                };
            }
        }
    }

    public class SessionCreateResponse
    {
        public bool Success { get; set; }
        public string? SessionId { get; set; }
        public string? CheckoutUrl { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class PaymentVerificationResult
    {
        public bool Success { get; set; }
        public string? PaymentIntentId { get; set; }
        public string? OrderNumber { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
