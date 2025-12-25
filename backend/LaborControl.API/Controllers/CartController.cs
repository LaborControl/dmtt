using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.DTOs;
using LaborControl.API.Models;
using System.Security.Claims;
using System.Text.Json;

namespace LaborControl.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CartController> _logger;

    public CartController(ApplicationDbContext context, ILogger<CartController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Récupère le panier du client connecté
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<CartResponse>> GetCart()
    {
        try
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            if (string.IsNullOrEmpty(customerIdClaim) || !Guid.TryParse(customerIdClaim, out var customerId))
            {
                return Unauthorized(new { message = "Client non authentifié" });
            }

            var cartItems = await _context.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.CustomerId == customerId)
                .OrderBy(ci => ci.CreatedAt)
                .ToListAsync();

            var cartItemDtos = cartItems.Select(ci =>
            {
                Dictionary<string, object>? metadata = null;
                if (!string.IsNullOrEmpty(ci.Metadata))
                {
                    try
                    {
                        metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(ci.Metadata);
                    }
                    catch { }
                }

                return new CartItemDTO
                {
                    Id = ci.Id,
                    ProductId = ci.ProductId,
                    ProductName = ci.Product?.Name ?? "",
                    ProductDescription = ci.Product?.Description,
                    ProductType = ci.Product?.ProductType ?? "",
                    Quantity = ci.Quantity,
                    UnitPrice = ci.UnitPrice,
                    TotalPrice = ci.UnitPrice * ci.Quantity,
                    ShippingCost = ci.Product?.ShippingCost,
                    ImageUrl = ci.Product?.ImageUrl,
                    Metadata = metadata
                };
            }).ToList();

            var subtotal = cartItemDtos.Sum(ci => ci.TotalPrice);
            var shippingTotal = cartItems
                .Select(ci => ci.Product?.ShippingCost ?? 0)
                .Where(cost => cost > 0)
                .Distinct()
                .Sum();

            var response = new CartResponse
            {
                Items = cartItemDtos,
                Subtotal = subtotal,
                ShippingTotal = shippingTotal,
                Total = subtotal + shippingTotal,
                ItemCount = cartItems.Sum(ci => ci.Quantity)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération du panier");
            return StatusCode(500, new { message = "Erreur lors de la récupération du panier" });
        }
    }

    /// <summary>
    /// Ajoute un produit au panier
    /// </summary>
    [HttpPost("items")]
    public async Task<ActionResult<CartItemDTO>> AddToCart([FromBody] AddToCartRequest request)
    {
        try
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            if (string.IsNullOrEmpty(customerIdClaim) || !Guid.TryParse(customerIdClaim, out var customerId))
            {
                return Unauthorized(new { message = "Client non authentifié" });
            }

            // Vérifier que le produit existe et est actif
            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null || !product.IsActive)
            {
                return BadRequest(new { message = "Produit non trouvé ou non disponible" });
            }

            // Vérifier si le produit est "one-time" et déjà acheté
            if (product.IsOneTimePerCustomer && product.ProductType == "pack_discovery")
            {
                var hasPackDiscovery = await _context.Orders
                    .AnyAsync(o => o.CustomerId == customerId &&
                                  o.ProductType == "pack_discovery" &&
                                  o.Status != "cancelled");

                if (hasPackDiscovery)
                {
                    return BadRequest(new { message = "Vous avez déjà commandé votre pack découverte" });
                }

                // Vérifier aussi s'il est déjà dans le panier
                var alreadyInCart = await _context.CartItems
                    .AnyAsync(ci => ci.CustomerId == customerId && ci.ProductId == request.ProductId);

                if (alreadyInCart)
                {
                    return BadRequest(new { message = "Le pack découverte est déjà dans votre panier" });
                }
            }

            // Vérifier le stock si applicable
            if (product.StockQuantity.HasValue && product.StockQuantity.Value < request.Quantity)
            {
                return BadRequest(new { message = $"Stock insuffisant. Disponible : {product.StockQuantity.Value}" });
            }

            // Vérifier si le produit est déjà dans le panier
            var existingCartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CustomerId == customerId && ci.ProductId == request.ProductId);

            if (existingCartItem != null)
            {
                // Mettre à jour la quantité
                existingCartItem.Quantity += request.Quantity;
                existingCartItem.UpdatedAt = DateTime.UtcNow;

                if (request.Metadata != null)
                {
                    existingCartItem.Metadata = JsonSerializer.Serialize(request.Metadata);
                }
            }
            else
            {
                // Créer un nouvel item
                var cartItem = new CartItem
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity,
                    UnitPrice = product.Price,
                    Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null,
                    CreatedAt = DateTime.UtcNow
                };

                _context.CartItems.Add(cartItem);
                existingCartItem = cartItem;
            }

            await _context.SaveChangesAsync();

            // Recharger avec le produit pour la réponse
            await _context.Entry(existingCartItem).Reference(ci => ci.Product).LoadAsync();

            Dictionary<string, object>? metadata = null;
            if (!string.IsNullOrEmpty(existingCartItem.Metadata))
            {
                try
                {
                    metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(existingCartItem.Metadata);
                }
                catch { }
            }

            var cartItemDto = new CartItemDTO
            {
                Id = existingCartItem.Id,
                ProductId = existingCartItem.ProductId,
                ProductName = existingCartItem.Product?.Name ?? "",
                ProductDescription = existingCartItem.Product?.Description,
                ProductType = existingCartItem.Product?.ProductType ?? "",
                Quantity = existingCartItem.Quantity,
                UnitPrice = existingCartItem.UnitPrice,
                TotalPrice = existingCartItem.UnitPrice * existingCartItem.Quantity,
                ShippingCost = existingCartItem.Product?.ShippingCost,
                ImageUrl = existingCartItem.Product?.ImageUrl,
                Metadata = metadata
            };

            return Ok(cartItemDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'ajout au panier");
            return StatusCode(500, new { message = "Erreur lors de l'ajout au panier" });
        }
    }

    /// <summary>
    /// Met à jour la quantité d'un article du panier
    /// </summary>
    [HttpPut("items/{id}")]
    public async Task<ActionResult<CartItemDTO>> UpdateCartItem(Guid id, [FromBody] UpdateCartItemRequest request)
    {
        try
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            if (string.IsNullOrEmpty(customerIdClaim) || !Guid.TryParse(customerIdClaim, out var customerId))
            {
                return Unauthorized(new { message = "Client non authentifié" });
            }

            var cartItem = await _context.CartItems
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.Id == id && ci.CustomerId == customerId);

            if (cartItem == null)
            {
                return NotFound(new { message = "Article non trouvé dans le panier" });
            }

            if (request.Quantity <= 0)
            {
                return BadRequest(new { message = "La quantité doit être supérieure à 0" });
            }

            // Vérifier le stock si applicable
            if (cartItem.Product?.StockQuantity.HasValue == true &&
                cartItem.Product.StockQuantity.Value < request.Quantity)
            {
                return BadRequest(new { message = $"Stock insuffisant. Disponible : {cartItem.Product.StockQuantity.Value}" });
            }

            cartItem.Quantity = request.Quantity;
            cartItem.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            Dictionary<string, object>? metadata = null;
            if (!string.IsNullOrEmpty(cartItem.Metadata))
            {
                try
                {
                    metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(cartItem.Metadata);
                }
                catch { }
            }

            var cartItemDto = new CartItemDTO
            {
                Id = cartItem.Id,
                ProductId = cartItem.ProductId,
                ProductName = cartItem.Product?.Name ?? "",
                ProductDescription = cartItem.Product?.Description,
                ProductType = cartItem.Product?.ProductType ?? "",
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.UnitPrice,
                TotalPrice = cartItem.UnitPrice * cartItem.Quantity,
                ShippingCost = cartItem.Product?.ShippingCost,
                ImageUrl = cartItem.Product?.ImageUrl,
                Metadata = metadata
            };

            return Ok(cartItemDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la mise à jour de l'article du panier");
            return StatusCode(500, new { message = "Erreur lors de la mise à jour de l'article" });
        }
    }

    /// <summary>
    /// Supprime un article du panier
    /// </summary>
    [HttpDelete("items/{id}")]
    public async Task<ActionResult> RemoveFromCart(Guid id)
    {
        try
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            if (string.IsNullOrEmpty(customerIdClaim) || !Guid.TryParse(customerIdClaim, out var customerId))
            {
                return Unauthorized(new { message = "Client non authentifié" });
            }

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.Id == id && ci.CustomerId == customerId);

            if (cartItem == null)
            {
                return NotFound(new { message = "Article non trouvé dans le panier" });
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Article supprimé du panier" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la suppression de l'article du panier");
            return StatusCode(500, new { message = "Erreur lors de la suppression de l'article" });
        }
    }

    /// <summary>
    /// Vide complètement le panier
    /// </summary>
    [HttpDelete]
    public async Task<ActionResult> ClearCart()
    {
        try
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            if (string.IsNullOrEmpty(customerIdClaim) || !Guid.TryParse(customerIdClaim, out var customerId))
            {
                return Unauthorized(new { message = "Client non authentifié" });
            }

            var cartItems = await _context.CartItems
                .Where(ci => ci.CustomerId == customerId)
                .ToListAsync();

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Panier vidé" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du vidage du panier");
            return StatusCode(500, new { message = "Erreur lors du vidage du panier" });
        }
    }
}
