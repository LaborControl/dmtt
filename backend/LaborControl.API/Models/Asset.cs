using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    public class Asset
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid ZoneId { get; set; }
        public Zone? Zone { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // "Échangeur E-101", "Chambre 201", "Compresseur C-01"
        
        [MaxLength(50)]
        public string? Code { get; set; } // "E-101", "CH-201", "C-01"
        
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty; // "EXCHANGER", "ROOM", "PUMP", "COMPRESSOR", "VALVE"
        
        [MaxLength(50)]
        public string? Category { get; set; } // "CRITICAL", "STANDARD", "SAFETY"
        
        [MaxLength(50)]
        public string? Status { get; set; } = "OPERATIONAL"; // "OPERATIONAL", "MAINTENANCE", "STOPPED"
        
        // Hiérarchie d'équipements (ex: Échangeur > Circuit primaire > Manomètre)
        public Guid? ParentAssetId { get; set; }
        public Asset? ParentAsset { get; set; }
        
        // Niveau dans la hiérarchie (0 = équipement principal, 1 = composant, etc.)
        public int Level { get; set; } = 0;

        // Ordre d'affichage
        public int DisplayOrder { get; set; } = 0;

        // Métadonnées techniques (JSON)
        public string? TechnicalData { get; set; } // Ex: {"pression_nominale": 10, "debit_max": 100}

        [Required]
        [MaxLength(50)]
        public string Manufacturer { get; set; } = ""; // "Schneider", "Siemens"

        [Required]
        [MaxLength(50)]
        public string Model { get; set; } = "";

        [MaxLength(50)]
        public string? SerialNumber { get; set; }

        public DateTime? InstallationDate { get; set; }

        /// <summary>
        /// Compteur d'heures de fonctionnement actuel de l'équipement
        /// Utilisé pour les maintenances préventives basées sur les heures
        /// </summary>
        public int OperatingHours { get; set; } = 0;

        /// <summary>
        /// Date de la dernière mise à jour du compteur d'heures
        /// </summary>
        public DateTime? LastOperatingHoursUpdate { get; set; }

        /// <summary>
        /// Équipement soumis à IA Cyrille pour obtenir ses préconisations de maintenance et son assistance au dépannage
        /// </summary>
        public bool IsAICyrilleEnabled { get; set; } = true;

        /// <summary>
        /// Équipement soumis à IA Aimée pour obtenir ses préconisations de planifications de maintenance
        /// </summary>
        public bool IsAIAimeeEnabled { get; set; } = true;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Relations
        public ICollection<Asset> SubAssets { get; set; } = new List<Asset>();
        public ICollection<ControlPoint> ControlPoints { get; set; } = new List<ControlPoint>();
    }
}