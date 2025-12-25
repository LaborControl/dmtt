using LaborControl.API.Models;

namespace LaborControl.API.Services
{
    /// <summary>
    /// Interface pour le service d'envoi d'emails
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Envoie un email
        /// </summary>
        /// <param name="message">Le message email à envoyer</param>
        /// <returns>True si l'email a été envoyé avec succès, False sinon</returns>
        Task<bool> SendEmailAsync(EmailMessage message);

        /// <summary>
        /// Envoie un email de bienvenue après inscription
        /// </summary>
        /// <param name="toEmail">Email du destinataire</param>
        /// <param name="userName">Nom de l'utilisateur</param>
        /// <returns>True si l'email a été envoyé avec succès</returns>
        Task<bool> SendWelcomeEmailAsync(string toEmail, string userName);

        /// <summary>
        /// Envoie une notification d'assignation de tâche
        /// </summary>
        /// <param name="toEmail">Email du destinataire</param>
        /// <param name="technicianName">Nom du technicien</param>
        /// <param name="taskName">Nom de la tâche</param>
        /// <param name="taskDescription">Description de la tâche</param>
        /// <param name="dueDate">Date d'échéance</param>
        /// <returns>True si l'email a été envoyé avec succès</returns>
        Task<bool> SendTaskAssignmentEmailAsync(string toEmail, string technicianName, string taskName, string taskDescription, DateTime dueDate);

        /// <summary>
        /// Envoie un rappel de maintenance
        /// </summary>
        /// <param name="toEmail">Email du destinataire</param>
        /// <param name="technicianName">Nom du technicien</param>
        /// <param name="maintenanceName">Nom de la maintenance</param>
        /// <param name="dueDate">Date d'échéance</param>
        /// <param name="daysUntilDue">Nombre de jours avant l'échéance</param>
        /// <returns>True si l'email a été envoyé avec succès</returns>
        Task<bool> SendMaintenanceReminderEmailAsync(string toEmail, string technicianName, string maintenanceName, DateTime dueDate, int daysUntilDue);

        /// <summary>
        /// Envoie une alerte de maintenance en retard
        /// </summary>
        /// <param name="toEmail">Email du destinataire</param>
        /// <param name="supervisorName">Nom du superviseur</param>
        /// <param name="maintenanceName">Nom de la maintenance</param>
        /// <param name="daysOverdue">Nombre de jours de retard</param>
        /// <returns>True si l'email a été envoyé avec succès</returns>
        Task<bool> SendMaintenanceOverdueAlertEmailAsync(string toEmail, string supervisorName, string maintenanceName, int daysOverdue);

        /// <summary>
        /// Envoie une confirmation de commande
        /// </summary>
        /// <param name="toEmail">Email du destinataire</param>
        /// <param name="customerName">Nom du client</param>
        /// <param name="orderNumber">Numéro de commande</param>
        /// <param name="totalAmount">Montant total</param>
        /// <returns>True si l'email a été envoyé avec succès</returns>
        Task<bool> SendOrderConfirmationEmailAsync(string toEmail, string customerName, string orderNumber, decimal totalAmount);

        /// <summary>
        /// Envoie un email de réinitialisation de mot de passe
        /// </summary>
        /// <param name="toEmail">Email du destinataire</param>
        /// <param name="resetLink">Lien de réinitialisation avec token</param>
        /// <returns>True si l'email a été envoyé avec succès</returns>
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink);

        /// <summary>
        /// Envoie une notification au superviseur qu'un utilisateur sans email souhaite réinitialiser son mot de passe
        /// </summary>
        /// <param name="supervisorEmail">Email du superviseur</param>
        /// <param name="userName">Nom d'utilisateur qui demande la réinitialisation</param>
        /// <returns>True si l'email a été envoyé avec succès</returns>
        Task<bool> SendSupervisorPasswordResetNotificationAsync(string supervisorEmail, string userName);

        /// <summary>
        /// Envoie une notification à l'admin lors de la création d'un nouveau compte client
        /// </summary>
        /// <param name="customerName">Nom de l'entreprise</param>
        /// <param name="contactName">Nom du contact</param>
        /// <param name="contactEmail">Email du contact</param>
        /// <param name="siret">SIRET de l'entreprise</param>
        /// <returns>True si l'email a été envoyé avec succès</returns>
        Task<bool> SendAdminNewAccountNotificationAsync(string customerName, string contactName, string contactEmail, string siret);

        /// <summary>
        /// Envoie une notification à l'admin lors de la création d'une nouvelle commande
        /// </summary>
        /// <param name="customerName">Nom du client</param>
        /// <param name="orderNumber">Numéro de commande</param>
        /// <param name="totalAmount">Montant total</param>
        /// <param name="chipsQuantity">Quantité de puces</param>
        /// <returns>True si l'email a été envoyé avec succès</returns>
        Task<bool> SendAdminNewOrderNotificationAsync(string customerName, string orderNumber, decimal totalAmount, int chipsQuantity);

        /// <summary>
        /// Envoie une notification au client que sa commande est prête à expédier
        /// </summary>
        /// <param name="toEmail">Email du client</param>
        /// <param name="customerName">Nom du client</param>
        /// <param name="orderNumber">Numéro de commande</param>
        /// <param name="packagingCode">Code PKG</param>
        /// <param name="chipsQuantity">Quantité de puces</param>
        /// <returns>True si l'email a été envoyé avec succès</returns>
        Task<bool> SendOrderReadyForShippingEmailAsync(string toEmail, string customerName, string orderNumber, string packagingCode, int chipsQuantity);

        /// <summary>
        /// Envoie une confirmation de paiement avec lien vers la facture
        /// </summary>
        /// <param name="toEmail">Email du client</param>
        /// <param name="customerName">Nom du client</param>
        /// <param name="orderNumber">Numéro de commande</param>
        /// <param name="orderId">ID de la commande pour le lien de téléchargement</param>
        /// <param name="totalAmount">Montant total payé</param>
        /// <param name="chipsQuantity">Quantité de puces commandées</param>
        /// <returns>True si l'email a été envoyé avec succès</returns>
        Task<bool> SendPaymentConfirmationEmailAsync(string toEmail, string customerName, string orderNumber, Guid orderId, decimal totalAmount, int chipsQuantity);

        /// <summary>
        /// Envoie une invitation à un nouveau collaborateur staff avec lien pour définir son mot de passe
        /// </summary>
        /// <param name="toEmail">Email du collaborateur</param>
        /// <param name="firstName">Prénom du collaborateur</param>
        /// <param name="lastName">Nom du collaborateur</param>
        /// <param name="token">Token sécurisé pour définir le mot de passe</param>
        /// <returns>True si l'email a été envoyé avec succès</returns>
        Task<bool> SendStaffInvitationEmailAsync(string toEmail, string firstName, string lastName, string token);

        /// <summary>
        /// Envoie un email de bienvenue à un nouvel employé avec ses identifiants de première connexion
        /// </summary>
        /// <param name="toEmail">Email de l'employé</param>
        /// <param name="firstName">Prénom de l'employé</param>
        /// <param name="lastName">Nom de l'employé</param>
        /// <param name="username">Pseudo de connexion généré</param>
        /// <param name="setupPin">Code PIN à 4 chiffres</param>
        /// <param name="pinExpiresAt">Date d'expiration du PIN</param>
        /// <returns>True si l'email a été envoyé avec succès</returns>
        Task<bool> SendNewEmployeeWelcomeEmailAsync(string toEmail, string firstName, string lastName, string username, string setupPin, DateTime pinExpiresAt);
    }
}
