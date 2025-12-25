using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaborControl.API.Models
{
    /// <summary>
    /// Représente le contenu éditable de la page d'accueil du site vitrine.
    /// Permet au SUPERADMIN de modifier le contenu sans redéploiement.
    /// Le contenu est stocké au format JSON pour une flexibilité maximale.
    /// </summary>
    public class HomeContent
    {
        /// <summary>
        /// Identifiant unique du contenu (UUID/Guid)
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Contenu structuré de la page d'accueil au format JSON (JSONB PostgreSQL).
        /// Contient toutes les sections éditables :
        /// - hero : Titre, sous-titre, 2 CTA
        /// - painPoints : Liste de 3-4 problèmes avec titre/description
        /// - solution : Titre, description, liste de features
        /// - testimonials : Liste de témoignages (nom, poste, entreprise, texte, photo)
        /// - faq : Liste de questions/réponses
        /// - pricing : Titre et plans avec nom/prix/features
        /// - footer : Texte, liens
        /// </summary>
        [Column(TypeName = "jsonb")]
        [Required]
        public string Content { get; set; } = "{}";

        /// <summary>
        /// Indique si ce contenu est actuellement publié et visible sur le site public.
        /// False = brouillon (visible uniquement pour SUPERADMIN)
        /// True = publié (visible pour tous les visiteurs)
        /// </summary>
        public bool IsPublished { get; set; } = false;

        /// <summary>
        /// Date et heure de publication du contenu (UTC).
        /// Null si le contenu n'a jamais été publié.
        /// </summary>
        public DateTime? PublishedAt { get; set; }

        /// <summary>
        /// Date et heure de la dernière modification du contenu (UTC).
        /// Mise à jour automatiquement à chaque modification.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date et heure de création du contenu (UTC).
        /// Définie une seule fois à la création.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Numéro de version du contenu pour l'historique.
        /// Incrémenté automatiquement à chaque modification.
        /// Permet de restaurer des versions antérieures si nécessaire.
        /// </summary>
        public int Version { get; set; } = 1;
    }
}
