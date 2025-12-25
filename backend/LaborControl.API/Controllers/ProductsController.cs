using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.DTOs;
using System.Security.Claims;
using System.Text.Json;

namespace LaborControl.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Récupère tous les produits actifs
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ProductListItemDTO>>> GetProducts()
    {
        try
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            if (string.IsNullOrEmpty(customerIdClaim) || !Guid.TryParse(customerIdClaim, out var customerId))
            {
                return Unauthorized(new { message = "Client non authentifié" });
            }

            var products = await _context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();

            // Vérifier si le client a déjà commandé le pack découverte
            var hasPackDiscovery = await _context.Orders
                .AnyAsync(o => o.CustomerId == customerId &&
                              o.ProductType == "pack_discovery" &&
                              o.Status != "cancelled");

            var productDtos = products.Select(p => new ProductListItemDTO
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                ProductType = p.ProductType,
                ShippingCost = p.ShippingCost,
                ImageUrl = p.ImageUrl,
                DisplayOrder = p.DisplayOrder,
                IsAvailable = !p.IsOneTimePerCustomer || !hasPackDiscovery || p.ProductType != "pack_discovery",
                AlreadyPurchased = p.IsOneTimePerCustomer && hasPackDiscovery && p.ProductType == "pack_discovery"
            }).ToList();

            return Ok(productDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des produits");
            return StatusCode(500, new { message = "Erreur lors de la récupération des produits" });
        }
    }

    /// <summary>
    /// Récupère un produit par son ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDTO>> GetProduct(Guid id)
    {
        try
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            if (string.IsNullOrEmpty(customerIdClaim) || !Guid.TryParse(customerIdClaim, out var customerId))
            {
                return Unauthorized(new { message = "Client non authentifié" });
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { message = "Produit non trouvé" });
            }

            // Vérifier si déjà acheté
            var alreadyPurchased = false;
            if (product.IsOneTimePerCustomer && product.ProductType == "pack_discovery")
            {
                alreadyPurchased = await _context.Orders
                    .AnyAsync(o => o.CustomerId == customerId &&
                                  o.ProductType == "pack_discovery" &&
                                  o.Status != "cancelled");
            }

            // Parser les métadonnées JSON
            Dictionary<string, object>? metadata = null;
            if (!string.IsNullOrEmpty(product.Metadata))
            {
                try
                {
                    metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(product.Metadata);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Impossible de parser les métadonnées du produit {ProductId}", id);
                }
            }

            var productDto = new ProductDTO
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                ProductType = product.ProductType,
                Category = product.Category,
                IsOneTimePerCustomer = product.IsOneTimePerCustomer,
                StockQuantity = product.StockQuantity,
                ShippingCost = product.ShippingCost,
                ImageUrl = product.ImageUrl,
                IsActive = product.IsActive,
                DisplayOrder = product.DisplayOrder,
                Metadata = metadata,
                IsAvailable = product.IsActive && (!product.IsOneTimePerCustomer || !alreadyPurchased),
                AlreadyPurchased = alreadyPurchased
            };

            return Ok(productDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération du produit {ProductId}", id);
            return StatusCode(500, new { message = "Erreur lors de la récupération du produit" });
        }
    }

    /// <summary>
    /// Récupère les produits disponibles pour le client connecté
    /// </summary>
    [HttpGet("available")]
    public async Task<ActionResult<AvailableProductsResponse>> GetAvailableProducts()
    {
        try
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            if (string.IsNullOrEmpty(customerIdClaim) || !Guid.TryParse(customerIdClaim, out var customerId))
            {
                return Unauthorized(new { message = "Client non authentifié" });
            }

            var products = await _context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();

            // Vérifier si le client a déjà commandé le pack découverte
            var hasPackDiscovery = await _context.Orders
                .AnyAsync(o => o.CustomerId == customerId &&
                              o.ProductType == "pack_discovery" &&
                              o.Status != "cancelled");

            var availableProducts = products
                .Where(p => !p.IsOneTimePerCustomer ||
                           p.ProductType != "pack_discovery" ||
                           !hasPackDiscovery)
                .Select(p => new ProductListItemDTO
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    ProductType = p.ProductType,
                    ShippingCost = p.ShippingCost,
                    ImageUrl = p.ImageUrl,
                    DisplayOrder = p.DisplayOrder,
                    IsAvailable = true,
                    AlreadyPurchased = false
                })
                .ToList();

            var response = new AvailableProductsResponse
            {
                Products = availableProducts,
                HasPackDiscovery = hasPackDiscovery
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des produits disponibles");
            return StatusCode(500, new { message = "Erreur lors de la récupération des produits disponibles" });
        }
    }
}
