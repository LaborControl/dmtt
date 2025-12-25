namespace LaborControl.API.DTOs;

/// <summary>
/// DTO pour un article dans le panier
/// </summary>
public class CartItemDTO
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductDescription { get; set; }
    public string ProductType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal? ShippingCost { get; set; }
    public string? ImageUrl { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Réponse complète du panier
/// </summary>
public class CartResponse
{
    public List<CartItemDTO> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal ShippingTotal { get; set; }
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
}

/// <summary>
/// Requête pour ajouter un produit au panier
/// </summary>
public class AddToCartRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Requête pour mettre à jour la quantité d'un article
/// </summary>
public class UpdateCartItemRequest
{
    public int Quantity { get; set; }
}
