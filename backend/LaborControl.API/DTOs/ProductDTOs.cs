using System.Text.Json;

namespace LaborControl.API.DTOs;

/// <summary>
/// DTO pour afficher un produit dans le catalogue
/// </summary>
public class ProductDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string ProductType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsOneTimePerCustomer { get; set; }
    public int? StockQuantity { get; set; }
    public decimal? ShippingCost { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }

    // Champs calculés
    public bool IsAvailable { get; set; }
    public bool AlreadyPurchased { get; set; }
}

/// <summary>
/// DTO léger pour la liste des produits
/// </summary>
public class ProductListItemDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string ProductType { get; set; } = string.Empty;
    public decimal? ShippingCost { get; set; }
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsAvailable { get; set; }
    public bool AlreadyPurchased { get; set; }
}

/// <summary>
/// Réponse avec les produits disponibles pour un client
/// </summary>
public class AvailableProductsResponse
{
    public List<ProductListItemDTO> Products { get; set; } = new();
    public bool HasPackDiscovery { get; set; }
}
