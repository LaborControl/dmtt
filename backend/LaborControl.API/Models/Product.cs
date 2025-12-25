using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models;

public class Product
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public decimal Price { get; set; }

    /// <summary>
    /// Type de produit: "pack_discovery", "nfc_chip", "subscription"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ProductType { get; set; } = string.Empty;

    /// <summary>
    /// Catégorie: "physical", "service", "subscription"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Est-ce un produit unique par client (comme le pack découverte) ?
    /// </summary>
    public bool IsOneTimePerCustomer { get; set; }

    /// <summary>
    /// Stock disponible (null = illimité)
    /// </summary>
    public int? StockQuantity { get; set; }

    /// <summary>
    /// Frais de livraison
    /// </summary>
    public decimal? ShippingCost { get; set; }

    /// <summary>
    /// Image du produit (URL)
    /// </summary>
    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Produit actif et disponible à la vente
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Ordre d'affichage
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Métadonnées JSON pour stocker des infos spécifiques au produit
    /// Exemple: {"points_included": 10, "chips_included": 10}
    /// </summary>
    [MaxLength(2000)]
    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
