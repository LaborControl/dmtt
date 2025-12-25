using LaborControl.API.Data;
using LaborControl.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LaborControl.API.Services
{
    public class OrderFulfillmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderFulfillmentService> _logger;
        private readonly IEmailService _emailService;

        public OrderFulfillmentService(ApplicationDbContext context, ILogger<OrderFulfillmentService> logger, IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        /// <summary>
        /// Génère les puces RFID pour une commande payée
        /// </summary>
        public async Task<bool> FulfillOrderAsync(Guid orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.RfidChips)
                    .Include(o => o.Customer)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    _logger.LogWarning($"Order {orderId} not found");
                    return false;
                }

                // Vérifier que la commande est payée
                if (order.Status != "PAID")
                {
                    _logger.LogWarning($"Order {orderId} is not paid (status: {order.Status})");
                    return false;
                }

                // Vérifier si les puces ont déjà été générées
                if (order.RfidChips.Any())
                {
                    _logger.LogWarning($"Order {orderId} already has {order.RfidChips.Count} chips generated");
                    return false;
                }

                // Générer le code packaging pour la commande si pas déjà fait
                if (string.IsNullOrEmpty(order.PackagingCode))
                {
                    order.PackagingCode = await GenerateUniquePackagingCodeAsync();
                }

                // Créer les puces RFID
                var chips = new List<RfidChip>();
                for (int i = 0; i < order.ChipsQuantity; i++)
                {
                    var chip = new RfidChip
                    {
                        Id = Guid.NewGuid(),
                        ChipId = GenerateChipId(),
                        Uid = GenerateUid(),
                        CustomerId = order.CustomerId,
                        OrderId = order.Id,
                        PackagingCode = $"{order.PackagingCode}-{(i + 1):D4}", // Ex: PKG-2025-001-0001
                        Status = "IN_TRANSIT",
                        Checksum = GenerateChecksum($"{order.PackagingCode}-{(i + 1):D4}"),
                        ActivationDate = DateTime.UtcNow
                    };
                    chips.Add(chip);
                }

                _context.RfidChips.AddRange(chips);
                order.UpdatedAt = DateTime.UtcNow;
                order.Status = "READY_TO_SHIP"; // Marquer comme prête à expédier

                await _context.SaveChangesAsync();

                // Envoyer un email au client pour l'informer que sa commande est prête
                try
                {
                    var customer = order.Customer;
                    if (customer != null && !string.IsNullOrEmpty(customer.ContactEmail))
                    {
                        await _emailService.SendOrderReadyForShippingEmailAsync(
                            customer.ContactEmail,
                            customer.Name,
                            order.OrderNumber,
                            order.PackagingCode,
                            chips.Count
                        );
                        _logger.LogInformation($"Order ready notification email sent to {customer.ContactEmail} for order {order.OrderNumber}");
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, $"Failed to send order ready notification email for order {orderId}");
                    // Continue même si l'email échoue
                }

                _logger.LogInformation($"Successfully fulfilled order {orderId} with {chips.Count} chips");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fulfilling order {orderId}");
                return false;
            }
        }

        /// <summary>
        /// Génère un code packaging unique pour une commande
        /// Format: PKG-{YYYY}-{SequentialNumber}
        /// </summary>
        private async Task<string> GenerateUniquePackagingCodeAsync()
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"PKG-{year}-";

            // Trouver le dernier code packaging de l'année
            var lastOrder = await _context.Orders
                .Where(o => o.PackagingCode != null && o.PackagingCode.StartsWith(prefix))
                .OrderByDescending(o => o.PackagingCode)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastOrder != null && !string.IsNullOrEmpty(lastOrder.PackagingCode))
            {
                // Extraire le numéro séquentiel
                var parts = lastOrder.PackagingCode.Split('-');
                if (parts.Length >= 3 && int.TryParse(parts[2], out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D3}"; // Ex: PKG-2025-001
        }

        /// <summary>
        /// Génère un ID de puce unique (peut être remplacé par un vrai UID NFC)
        /// </summary>
        private string GenerateChipId()
        {
            return $"CHIP-{Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper()}";
        }

        /// <summary>
        /// Génère un UID NFC simulé (sera remplacé par le vrai UID lors du scan)
        /// </summary>
        private string GenerateUid()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper();
        }

        /// <summary>
        /// Génère un checksum SHA256 pour validation
        /// </summary>
        private string GenerateChecksum(string input)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(input);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Réceptionne une commande côté client avec le code packaging
        /// </summary>
        public async Task<(bool Success, string Message, int ChipsReceived)> ReceiveOrderAsync(Guid orderId, string packagingCode, Guid customerId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.RfidChips)
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customerId);

                if (order == null)
                {
                    return (false, "Commande introuvable", 0);
                }

                // Vérifier que la commande est bien expédiée/payée
                if (order.Status != "SHIPPED" && order.Status != "PAID")
                {
                    return (false, $"La commande n'est pas encore expédiée (statut: {order.Status})", 0);
                }

                // Vérifier le code packaging
                if (string.IsNullOrEmpty(order.PackagingCode))
                {
                    return (false, "Cette commande n'a pas de code packaging", 0);
                }

                if (!order.PackagingCode.Equals(packagingCode, StringComparison.OrdinalIgnoreCase))
                {
                    return (false, "Code packaging incorrect", 0);
                }

                // Vérifier si déjà réceptionnée
                if (order.Status == "DELIVERED")
                {
                    return (false, "Cette commande a déjà été réceptionnée", 0);
                }

                // CORRECTION WORKFLOW: Ne PAS activer les puces à la réception!
                // Les puces restent EN_STOCK jusqu'au 1er scan NFC avec vérification d'authenticité
                // La réception du colis (via code PKG) valide UNIQUEMENT la livraison physique
                // L'activation + affectation à la whitelist se fait lors du scan NFC (endpoint /api/rfidchips/activate-chip)

                var receivedDate = DateTime.UtcNow;

                // Mettre à jour la commande (DELIVERED = client a saisi le code PKG, colis reçu)
                order.Status = "DELIVERED";
                order.DeliveredAt = receivedDate;
                order.UpdatedAt = receivedDate;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Order {orderId} received by customer {customerId} with {order.RfidChips.Count} chips");
                return (true, "Commande réceptionnée avec succès", order.RfidChips.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error receiving order {orderId}");
                return (false, "Erreur lors de la réception", 0);
            }
        }
    }
}
