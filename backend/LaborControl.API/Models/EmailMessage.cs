namespace LaborControl.API.Models
{
    /// <summary>
    /// Modèle pour représenter un email à envoyer
    /// </summary>
    public class EmailMessage
    {
        /// <summary>
        /// Adresse email du destinataire
        /// </summary>
        public string ToEmail { get; set; } = string.Empty;

        /// <summary>
        /// Nom du destinataire (optionnel)
        /// </summary>
        public string? ToName { get; set; }

        /// <summary>
        /// Sujet de l'email
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Corps de l'email en HTML
        /// </summary>
        public string HtmlContent { get; set; } = string.Empty;

        /// <summary>
        /// Corps de l'email en texte brut (optionnel)
        /// </summary>
        public string? PlainTextContent { get; set; }

        /// <summary>
        /// Adresse email de l'expéditeur (optionnel, utilise le défaut si non fourni)
        /// </summary>
        public string? FromEmail { get; set; }

        /// <summary>
        /// Nom de l'expéditeur (optionnel)
        /// </summary>
        public string? FromName { get; set; }

        /// <summary>
        /// Adresses email en copie (optionnel)
        /// </summary>
        public List<string>? CcEmails { get; set; }

        /// <summary>
        /// Adresses email en copie cachée (optionnel)
        /// </summary>
        public List<string>? BccEmails { get; set; }

        /// <summary>
        /// Adresse email de réponse (optionnel)
        /// </summary>
        public string? ReplyToEmail { get; set; }
    }
}
