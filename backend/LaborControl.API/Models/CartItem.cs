using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaborControl.API.Models;

public class CartItem
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }

    [Required]
    public Guid ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    [Required]
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Prix unitaire au moment de l'ajout au panier
    /// </summary>
    [Required]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Métadonnées JSON pour options spécifiques
    /// Exemple: {"delivery_service": "Direction", "notes": "..."}
    /// </summary>
    [MaxLength(2000)]
    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
