using LaborControl.API.Data;
using LaborControl.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LaborControl.API.Services
{
    /// <summary>
    /// Service de gestion du stock de puces RFID
    /// Gère le calcul du stock disponible avec réservation sans scan à la préparation
    /// </summary>
    public class StockManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StockManagementService> _logger;

        public StockManagementService(ApplicationDbContext context, ILogger<StockManagementService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Calcule le stock total de puces EN_STOCK (stock physique total)
        /// </summary>
        public async Task<int> GetTotalStockAsync()
        {
            return await _context.RfidChips
                .CountAsync(c => c.Status == "EN_STOCK" && c.CustomerId == null);
        }

        /// <summary>
        /// Calcule le stock réservé (puces sorties physiquement mais pas encore scannées par client)
        /// </summary>
        public async Task<int> GetReservedStockAsync()
        {
            return await _context.Orders
                .Where(o => o.IsStockReserved && o.Status != "CANCELLED")
                .SumAsync(o => o.ChipsQuantity);
        }

        /// <summary>
        /// Calcule le stock disponible (stock physique - réservations)
        /// </summary>
        public async Task<int> GetAvailableStockAsync()
        {
            var totalStock = await GetTotalStockAsync();
            var reservedStock = await GetReservedStockAsync();
            return totalStock - reservedStock;
        }

        /// <summary>
        /// Retourne les statistiques complètes du stock
        /// </summary>
        public async Task<StockStatistics> GetStockStatisticsAsync()
        {
            var totalStock = await GetTotalStockAsync();
            var reservedStock = await GetReservedStockAsync();
            var availableStock = totalStock - reservedStock;

            var stats = new StockStatistics
            {
                TotalStock = totalStock,
                ReservedStock = reservedStock,
                AvailableStock = availableStock,
                EnTransit = await _context.RfidChips.CountAsync(c => c.Status == "EN_TRANSIT"),
                EnAtelier = await _context.RfidChips.CountAsync(c => c.Status == "EN_ATELIER"),
                TotalInactive = await _context.RfidChips.CountAsync(c => c.Status == "INACTIVE"),
                TotalActive = await _context.RfidChips.CountAsync(c => c.Status == "ACTIVE"),
                RetourSav = await _context.RfidChips.CountAsync(c => c.Status == "RETOUR_SAV"),
                ReceptionSav = await _context.RfidChips.CountAsync(c => c.Status == "RECEPTION_SAV")
            };

            _logger.LogInformation(
                "[STOCK] Total: {Total}, Réservé: {Reserved}, Disponible: {Available}",
                totalStock, reservedStock, availableStock
            );

            return stats;
        }

        /// <summary>
        /// Vérifie si le stock disponible est suffisant pour une quantité donnée
        /// </summary>
        public async Task<bool> IsStockAvailableAsync(int quantity)
        {
            var availableStock = await GetAvailableStockAsync();
            return availableStock >= quantity;
        }

        /// <summary>
        /// Réserve le stock pour une commande (marque IsStockReserved = true)
        /// </summary>
        public async Task<(bool Success, string Message)> ReserveStockForOrderAsync(Guid orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return (false, "Commande introuvable");

            if (order.IsStockReserved)
                return (false, "Stock déjà réservé pour cette commande");

            // Vérifier stock disponible
            var availableStock = await GetAvailableStockAsync();
            if (availableStock < order.ChipsQuantity)
            {
                _logger.LogWarning(
                    "[STOCK] Stock insuffisant pour Order {OrderId}: {Required} demandées, {Available} disponibles",
                    orderId, order.ChipsQuantity, availableStock
                );
                return (false, $"Stock insuffisant: {availableStock} puces disponibles, {order.ChipsQuantity} requises");
            }

            // Marquer le stock comme réservé
            order.IsStockReserved = true;
            order.PreparedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "[STOCK] Réservation OK pour Order {OrderId}: {Quantity} puces réservées",
                orderId, order.ChipsQuantity
            );

            return (true, $"{order.ChipsQuantity} puces réservées avec succès");
        }

        /// <summary>
        /// Libère la réservation de stock pour une commande annulée
        /// </summary>
        public async Task<bool> ReleaseStockReservationAsync(Guid orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            if (order.IsStockReserved)
            {
                order.IsStockReserved = false;
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "[STOCK] Libération réservation pour Order {OrderId}: {Quantity} puces libérées",
                    orderId, order.ChipsQuantity
                );

                return true;
            }

            return false;
        }

        /// <summary>
        /// Vérifie automatiquement si une commande a toutes ses puces scannées
        /// Si oui, met IsStockReserved = false automatiquement
        /// </summary>
        public async System.Threading.Tasks.Task CheckAndReleaseCompletedOrderAsync(Guid orderId)
        {
            var order = await _context.Orders
                .Include(o => o.RfidChips)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null || !order.IsStockReserved) return;

            // Compter les puces assignées à cette commande
            var assignedCount = order.RfidChips.Count(c => c.OrderId == orderId);

            if (assignedCount >= order.ChipsQuantity)
            {
                order.IsStockReserved = false;
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "[STOCK] Commande {OrderId} complète: {Count}/{Total} puces scannées, réservation libérée",
                    orderId, assignedCount, order.ChipsQuantity
                );
            }
        }
    }

    /// <summary>
    /// Statistiques du stock de puces RFID
    /// </summary>
    public class StockStatistics
    {
        public int TotalStock { get; set; }         // Puces EN_STOCK (physique)
        public int ReservedStock { get; set; }      // Puces réservées (IsStockReserved = true)
        public int AvailableStock { get; set; }     // Stock disponible (Total - Réservé)
        public int EnTransit { get; set; }          // En transit fournisseur
        public int EnAtelier { get; set; }          // En atelier (non encodées)
        public int TotalInactive { get; set; }      // Chez clients (non affectées)
        public int TotalActive { get; set; }        // Chez clients (affectées)
        public int RetourSav { get; set; }          // En retour SAV
        public int ReceptionSav { get; set; }       // SAV reçues
    }
}
