using Azure;
using Azure.Communication.Email;
using LaborControl.API.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CustomEmailMessage = LaborControl.API.Models.EmailMessage;
using AzureEmailMessage = Azure.Communication.Email.EmailMessage;

namespace LaborControl.API.Services
{
    /// <summary>
    /// Service d'envoi d'emails via Azure Communication Services
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly EmailClient _emailClient;
        private readonly string _senderEmail;
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            try
            {
                // Lire les variables d'environnement directement (workaround pour problème de chargement IConfiguration)
                // Azure App Service injecte les paramètres comme variables d'environnement
                var connectionString = Environment.GetEnvironmentVariable("AzureCommunicationServices__ConnectionString")
                    ?? configuration["AzureCommunicationServices__ConnectionString"];

                // Récupérer l'email de l'expéditeur
                _senderEmail = Environment.GetEnvironmentVariable("AzureCommunicationServices__SenderEmail")
                    ?? configuration["AzureCommunicationServices__SenderEmail"]
                    ?? "DoNotReply@azurecomm.net";

                // Initialiser le client Email seulement si la connection string est disponible
                if (!string.IsNullOrEmpty(connectionString))
                {
                    _emailClient = new EmailClient(connectionString);
                    _logger.LogInformation("EmailService initialisé avec Azure Communication Services");
                }
                else
                {
                    _emailClient = null;
                    _logger.LogWarning("AzureCommunicationServices__ConnectionString n'est pas configurée - les emails ne seront pas envoyés");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'initialisation du EmailService");
                _emailClient = null;
                _senderEmail = "DoNotReply@azurecomm.net";
            }
        }

        /// <summary>
        /// Envoie un email générique
        /// </summary>
        public async Task<bool> SendEmailAsync(CustomEmailMessage message)
        {
            try
            {
                // Si le client email n'est pas initialisé, retourner false
                if (_emailClient == null)
                {
                    _logger.LogWarning($"Email non envoyé à {message.ToEmail} - Azure Communication Services non configuré");
                    return false;
                }

                // Valider l'email de destination
                if (string.IsNullOrEmpty(message.ToEmail))
                {
                    _logger.LogWarning("Email non envoyé - adresse email de destination vide");
                    return false;
                }

                // Utiliser l'email de l'expéditeur fourni ou le défaut
                var fromEmail = message.FromEmail ?? _senderEmail;
                var fromName = message.FromName ?? "Labor Control";

                // Créer la liste des destinataires
                var toRecipients = new List<EmailAddress>
                {
                    new EmailAddress(message.ToEmail, message.ToName ?? "Destinataire")
                };

                // Créer la liste des CC si fournis
                var ccRecipients = new List<EmailAddress>();
                if (message.CcEmails != null && message.CcEmails.Any())
                {
                    ccRecipients.AddRange(message.CcEmails.Select(cc => new EmailAddress(cc)));
                }

                // Créer la liste des BCC si fournis
                var bccRecipients = new List<EmailAddress>();
                if (message.BccEmails != null && message.BccEmails.Any())
                {
                    bccRecipients.AddRange(message.BccEmails.Select(bcc => new EmailAddress(bcc)));
                }

                // Créer le contenu de l'email
                var emailContent = new EmailContent(message.Subject ?? "Sans sujet")
                {
                    PlainText = message.PlainTextContent ?? StripHtmlTags(message.HtmlContent),
                    Html = message.HtmlContent
                };

                // Créer le message email
                // Note: Azure Communication Services nécessite juste l'adresse email, pas le format "Name <email>"
                var emailMessage = new AzureEmailMessage(
                    senderAddress: fromEmail,
                    content: emailContent,
                    recipients: new EmailRecipients(toRecipients, ccRecipients, bccRecipients)
                );

                // Envoyer l'email
                EmailSendOperation sendOperation = await _emailClient.SendAsync(
                    WaitUntil.Completed,
                    emailMessage
                );

                _logger.LogInformation($"Email envoyé avec succès à {message.ToEmail}. ID: {sendOperation.Id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'envoi de l'email à {message.ToEmail}");
                return false;
            }
        }

        /// <summary>
        /// Envoie un email de bienvenue
        /// </summary>
        public async Task<bool> SendWelcomeEmailAsync(string toEmail, string userName)
        {
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #0066cc; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; border-radius: 0 0 5px 5px; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Bienvenue sur Labor Control</h1>
        </div>
        <div class='content'>
            <p>Bonjour {userName},</p>
            <p>Merci de vous être inscrit sur <strong>Labor Control</strong>, la plateforme de gestion des tâches de maintenance industrielle.</p>
            <p>Votre compte a été créé avec succès. Vous pouvez maintenant :</p>
            <ul>
                <li>Accéder à votre tableau de bord</li>
                <li>Créer et gérer vos tâches de maintenance</li>
                <li>Assigner des tâches à votre équipe</li>
                <li>Suivre l'avancement de vos interventions</li>
            </ul>
            <p>Si vous avez des questions, n'hésitez pas à nous contacter.</p>
            <p>Cordialement,<br>L'équipe Labor Control</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 Labor Control. Tous droits réservés.</p>
        </div>
    </div>
</body>
</html>";

            var message = new CustomEmailMessage
            {
                ToEmail = toEmail,
                ToName = userName,
                Subject = "Bienvenue sur Labor Control",
                HtmlContent = htmlContent
            };

            return await SendEmailAsync(message);
        }

        /// <summary>
        /// Envoie une notification d'assignation de tâche
        /// </summary>
        public async Task<bool> SendTaskAssignmentEmailAsync(string toEmail, string technicianName, string taskName, string taskDescription, DateTime dueDate)
        {
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #ff9800; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; border-radius: 0 0 5px 5px; }}
        .task-details {{ background-color: #fff; padding: 15px; border-left: 4px solid #ff9800; margin: 15px 0; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Nouvelle tâche assignée</h1>
        </div>
        <div class='content'>
            <p>Bonjour {technicianName},</p>
            <p>Une nouvelle tâche de maintenance vous a été assignée.</p>
            <div class='task-details'>
                <p><strong>Tâche :</strong> {taskName}</p>
                <p><strong>Description :</strong> {taskDescription}</p>
                <p><strong>Date d'échéance :</strong> {dueDate:dd/MM/yyyy HH:mm}</p>
            </div>
            <p>Veuillez vous connecter à votre compte Labor Control pour consulter les détails complets et mettre à jour le statut de la tâche.</p>
            <p>Cordialement,<br>L'équipe Labor Control</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 Labor Control. Tous droits réservés.</p>
        </div>
    </div>
</body>
</html>";

            var message = new CustomEmailMessage
            {
                ToEmail = toEmail,
                ToName = technicianName,
                Subject = $"Nouvelle tâche assignée : {taskName}",
                HtmlContent = htmlContent
            };

            return await SendEmailAsync(message);
        }

        /// <summary>
        /// Envoie un rappel de maintenance
        /// </summary>
        public async Task<bool> SendMaintenanceReminderEmailAsync(string toEmail, string technicianName, string maintenanceName, DateTime dueDate, int daysUntilDue)
        {
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #2196F3; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; border-radius: 0 0 5px 5px; }}
        .alert {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 15px 0; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Rappel de maintenance</h1>
        </div>
        <div class='content'>
            <p>Bonjour {technicianName},</p>
            <p>Ceci est un rappel que la maintenance suivante est prévue dans <strong>{daysUntilDue} jour(s)</strong> :</p>
            <div class='alert'>
                <p><strong>Maintenance :</strong> {maintenanceName}</p>
                <p><strong>Date prévue :</strong> {dueDate:dd/MM/yyyy HH:mm}</p>
            </div>
            <p>Veuillez vous assurer que vous êtes disponible et que vous avez tous les outils nécessaires pour effectuer cette maintenance.</p>
            <p>Cordialement,<br>L'équipe Labor Control</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 Labor Control. Tous droits réservés.</p>
        </div>
    </div>
</body>
</html>";

            var message = new CustomEmailMessage
            {
                ToEmail = toEmail,
                ToName = technicianName,
                Subject = $"Rappel : Maintenance {maintenanceName} dans {daysUntilDue} jour(s)",
                HtmlContent = htmlContent
            };

            return await SendEmailAsync(message);
        }

        /// <summary>
        /// Envoie une alerte de maintenance en retard
        /// </summary>
        public async Task<bool> SendMaintenanceOverdueAlertEmailAsync(string toEmail, string supervisorName, string maintenanceName, int daysOverdue)
        {
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #f44336; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; border-radius: 0 0 5px 5px; }}
        .alert {{ background-color: #ffebee; border-left: 4px solid #f44336; padding: 15px; margin: 15px 0; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>⚠️ Alerte : Maintenance en retard</h1>
        </div>
        <div class='content'>
            <p>Bonjour {supervisorName},</p>
            <p>Une maintenance est <strong>en retard de {daysOverdue} jour(s)</strong> :</p>
            <div class='alert'>
                <p><strong>Maintenance :</strong> {maintenanceName}</p>
                <p><strong>Retard :</strong> {daysOverdue} jour(s)</p>
            </div>
            <p>Veuillez prendre les mesures nécessaires pour que cette maintenance soit effectuée au plus tôt.</p>
            <p>Cordialement,<br>L'équipe Labor Control</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 Labor Control. Tous droits réservés.</p>
        </div>
    </div>
</body>
</html>";

            var message = new CustomEmailMessage
            {
                ToEmail = toEmail,
                ToName = supervisorName,
                Subject = $"⚠️ ALERTE : Maintenance {maintenanceName} en retard de {daysOverdue} jour(s)",
                HtmlContent = htmlContent
            };

            return await SendEmailAsync(message);
        }

        /// <summary>
        /// Envoie une confirmation de commande
        /// </summary>
        public async Task<bool> SendOrderConfirmationEmailAsync(string toEmail, string customerName, string orderNumber, decimal totalAmount)
        {
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; border-radius: 0 0 5px 5px; }}
        .order-details {{ background-color: #fff; padding: 15px; border-left: 4px solid #4CAF50; margin: 15px 0; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✓ Commande confirmée</h1>
        </div>
        <div class='content'>
            <p>Bonjour {customerName},</p>
            <p>Merci pour votre commande ! Voici les détails :</p>
            <div class='order-details'>
                <p><strong>Numéro de commande :</strong> {orderNumber}</p>
                <p><strong>Montant total :</strong> {totalAmount:C}</p>
            </div>
            <p>Vous recevrez un email de suivi dès que votre commande sera expédiée.</p>
            <p>Si vous avez des questions, n'hésitez pas à nous contacter.</p>
            <p>Cordialement,<br>L'équipe Labor Control</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 Labor Control. Tous droits réservés.</p>
        </div>
    </div>
</body>
</html>";

            var message = new CustomEmailMessage
            {
                ToEmail = toEmail,
                ToName = customerName,
                Subject = $"Confirmation de commande #{orderNumber}",
                HtmlContent = htmlContent
            };

            return await SendEmailAsync(message);
        }

        /// <summary>
        /// Envoie un email de réinitialisation de mot de passe
        /// </summary>
        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; border-radius: 0 0 5px 5px; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; margin: 15px 0; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Réinitialisation de mot de passe</h1>
        </div>
        <div class='content'>
            <p>Bonjour,</p>
            <p>Vous avez demandé la réinitialisation de votre mot de passe Labor Control.</p>
            <p>Cliquez sur le bouton ci-dessous pour définir un nouveau mot de passe :</p>
            <p style='text-align: center;'>
                <a href='{resetLink}' class='button'>Réinitialiser mon mot de passe</a>
            </p>
            <p>Ou copiez ce lien dans votre navigateur :</p>
            <p style='word-break: break-all; color: #0066cc;'>{resetLink}</p>
            <div class='warning'>
                <p><strong>Important :</strong> Ce lien est valable pendant 24 heures.</p>
                <p>Si vous n'avez pas demandé cette réinitialisation, ignorez simplement cet email.</p>
            </div>
            <p>Cordialement,<br>L'équipe Labor Control</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 Labor Control. Tous droits réservés.</p>
        </div>
    </div>
</body>
</html>";

            var message = new CustomEmailMessage
            {
                ToEmail = toEmail,
                Subject = "Réinitialisation de votre mot de passe Labor Control",
                HtmlContent = htmlContent
            };

            return await SendEmailAsync(message);
        }

        /// <summary>
        /// Envoie une notification au superviseur qu'un utilisateur sans email souhaite réinitialiser son mot de passe
        /// </summary>
        public async Task<bool> SendSupervisorPasswordResetNotificationAsync(string supervisorEmail, string userName)
        {
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #ff9800; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; border-radius: 0 0 5px 5px; }}
        .alert {{ background-color: #fff3cd; border-left: 4px solid #ff9800; padding: 15px; margin: 15px 0; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Demande de réinitialisation de mot de passe</h1>
        </div>
        <div class='content'>
            <p>Bonjour,</p>
            <p>Un utilisateur de votre équipe a demandé la réinitialisation de son mot de passe :</p>
            <div class='alert'>
                <p><strong>Utilisateur :</strong> {userName}</p>
            </div>
            <p>Cet utilisateur n'a pas d'adresse email configurée dans le système. En tant que superviseur, veuillez :</p>
            <ol>
                <li>Contacter l'utilisateur pour vérifier qu'il a bien fait cette demande</li>
                <li>Vous connecter au système Labor Control</li>
                <li>Réinitialiser manuellement le mot de passe de l'utilisateur dans les paramètres</li>
                <li>Communiquer le nouveau mot de passe à l'utilisateur de manière sécurisée</li>
            </ol>
            <p>Si vous n'êtes pas au courant de cette demande, veuillez contacter votre administrateur.</p>
            <p>Cordialement,<br>L'équipe Labor Control</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 Labor Control. Tous droits réservés.</p>
        </div>
    </div>
</body>
</html>";

            var message = new CustomEmailMessage
            {
                ToEmail = supervisorEmail,
                Subject = $"Demande de réinitialisation de mot de passe - {userName}",
                HtmlContent = htmlContent
            };

            return await SendEmailAsync(message);
        }

        /// <summary>
        /// Envoie une notification à l'admin lors de la création d'un nouveau compte client
        /// </summary>
        public async Task<bool> SendAdminNewAccountNotificationAsync(string customerName, string contactName, string contactEmail, string siret)
        {
            try
            {
                var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; border-radius: 0 0 5px 5px; }}
        .customer-details {{ background-color: #fff; padding: 15px; border-left: 4px solid #4CAF50; margin: 15px 0; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Nouveau compte client créé</h1>
        </div>
        <div class='content'>
            <p>Bonjour Admin,</p>
            <p>Un nouveau compte client professionnel vient d'être créé sur Labor Control :</p>
            <div class='customer-details'>
                <p><strong>Entreprise :</strong> {customerName}</p>
                <p><strong>Contact :</strong> {contactName}</p>
                <p><strong>Email :</strong> {contactEmail}</p>
                <p><strong>SIRET :</strong> {siret}</p>
            </div>
            <p>Connectez-vous à l'interface d'administration pour gérer ce nouveau client.</p>
            <p>Cordialement,<br>Système Labor Control</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 Labor Control. Tous droits réservés.</p>
        </div>
    </div>
</body>
</html>";

                var message = new CustomEmailMessage
                {
                    ToEmail = "contact@labor-control.fr",
                    ToName = "Admin Labor Control",
                    Subject = "🎉 Nouveau compte client créé",
                    HtmlContent = htmlContent
                };

                var result = await SendEmailAsync(message);

                if (result)
                {
                    _logger.LogInformation($"[ADMIN EMAIL] Notification envoyée : nouveau compte client {customerName}");
                }
                else
                {
                    _logger.LogWarning($"[ADMIN EMAIL] Échec notification nouveau compte client {customerName}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[ADMIN EMAIL] Erreur lors de l'envoi de la notification nouveau compte {customerName}");
                return false;
            }
        }

        /// <summary>
        /// Envoie une notification à l'admin lors de la création d'une nouvelle commande
        /// </summary>
        public async Task<bool> SendAdminNewOrderNotificationAsync(string customerName, string orderNumber, decimal totalAmount, int chipsQuantity)
        {
            try
            {
                var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #FF9800; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; border-radius: 0 0 5px 5px; }}
        .order-details {{ background-color: #fff; padding: 15px; border-left: 4px solid #FF9800; margin: 15px 0; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Nouvelle commande</h1>
        </div>
        <div class='content'>
            <p>Bonjour Admin,</p>
            <p>Une nouvelle commande vient d'être créée sur Labor Control :</p>
            <div class='order-details'>
                <p><strong>Client :</strong> {customerName}</p>
                <p><strong>Numéro de commande :</strong> {orderNumber}</p>
                <p><strong>Montant total :</strong> {totalAmount:C}</p>
                <p><strong>Quantité de puces :</strong> {chipsQuantity}</p>
            </div>
            <p>Connectez-vous à l'interface d'administration pour traiter cette commande.</p>
            <p>Cordialement,<br>Système Labor Control</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 Labor Control. Tous droits réservés.</p>
        </div>
    </div>
</body>
</html>";

                var message = new CustomEmailMessage
                {
                    ToEmail = "contact@labor-control.fr",
                    ToName = "Admin Labor Control",
                    Subject = "🛒 Nouvelle commande",
                    HtmlContent = htmlContent
                };

                var result = await SendEmailAsync(message);

                if (result)
                {
                    _logger.LogInformation($"[ADMIN EMAIL] Notification envoyée : nouvelle commande {orderNumber} de {customerName}");
                }
                else
                {
                    _logger.LogWarning($"[ADMIN EMAIL] Échec notification nouvelle commande {orderNumber}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[ADMIN EMAIL] Erreur lors de l'envoi de la notification nouvelle commande {orderNumber}");
                return false;
            }
        }

        public async Task<bool> SendOrderReadyForShippingEmailAsync(string toEmail, string customerName, string orderNumber, string packagingCode, int chipsQuantity)
        {
            try
            {
                var htmlContent = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .info-box {{ background: white; padding: 20px; border-left: 4px solid #667eea; margin: 20px 0; }}
                        .pkg-code {{ font-size: 24px; font-weight: bold; color: #667eea; text-align: center; padding: 15px; background: #f0f0f0; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>📦 Votre commande est prête !</h1>
                        </div>
                        <div class='content'>
                            <p>Bonjour {customerName},</p>

                            <p>Bonne nouvelle ! Votre commande <strong>{orderNumber}</strong> a été préparée et est prête à être expédiée.</p>

                            <div class='info-box'>
                                <h3>Détails de votre commande :</h3>
                                <ul>
                                    <li><strong>Numéro de commande :</strong> {orderNumber}</li>
                                    <li><strong>Quantité de puces :</strong> {chipsQuantity}</li>
                                    <li><strong>Code de packaging (PKG) :</strong></li>
                                </ul>

                                <div class='pkg-code'>{packagingCode}</div>

                                <p style='color: #666; font-size: 0.9em;'>
                                    ℹ️ Ce code PKG sera nécessaire pour réceptionner votre commande dans l'application.
                                </p>
                            </div>

                            <p>Votre commande sera exp édiée très prochainement. Vous recevrez un email de confirmation d'expédition avec le numéro de suivi dès que le colis sera pris en charge par le transporteur.</p>

                            <p>Pour toute question, n'hésitez pas à nous contacter.</p>

                            <p>Cordialement,<br>L'équipe Labor Control</p>
                        </div>
                        <div class='footer'>
                            <p>Cet email a été envoyé automatiquement, merci de ne pas y répondre.</p>
                            <p>&copy; 2025 Labor Control - Tous droits réservés</p>
                        </div>
                    </div>
                </body>
                </html>";

                var message = new CustomEmailMessage
                {
                    ToEmail = toEmail,
                    ToName = customerName,
                    Subject = $"📦 Votre commande {orderNumber} est prête !",
                    HtmlContent = htmlContent
                };

                var result = await SendEmailAsync(message);

                if (result)
                {
                    _logger.LogInformation($"[ORDER READY] Email envoyé à {toEmail} pour la commande {orderNumber}");
                }
                else
                {
                    _logger.LogWarning($"[ORDER READY] Échec de l'envoi à {toEmail} pour la commande {orderNumber}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[ORDER READY] Erreur lors de l'envoi de l'email pour la commande {orderNumber}");
                return false;
            }
        }

        /// <summary>
        /// Envoie une confirmation de paiement avec lien vers la facture
        /// </summary>
        public async Task<bool> SendPaymentConfirmationEmailAsync(string toEmail, string customerName, string orderNumber, Guid orderId, decimal totalAmount, int chipsQuantity)
        {
            try
            {
                // Récupérer l'URL de base de l'API
                var apiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL")
                    ?? _configuration["API_BASE_URL"]
                    ?? "https://labor-control-api.azurewebsites.net";

                var invoiceDownloadLink = $"{apiBaseUrl}/api/payments/invoice/{orderId}";

                var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .checkmark {{ font-size: 48px; margin-bottom: 10px; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border: 1px solid #ddd; }}
        .order-details {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #10b981; }}
        .order-details h3 {{ margin-top: 0; color: #10b981; }}
        .detail-row {{ display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px solid #eee; }}
        .detail-label {{ font-weight: 600; color: #666; }}
        .detail-value {{ color: #333; }}
        .total {{ font-size: 20px; font-weight: bold; color: #10b981; }}
        .invoice-section {{ text-align: center; margin: 30px 0; padding: 25px; background: linear-gradient(135deg, #f0fdf4 0%, #dcfce7 100%); border-radius: 8px; }}
        .invoice-button {{ display: inline-block; padding: 15px 40px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 16px; margin-top: 15px; box-shadow: 0 4px 6px rgba(16, 185, 129, 0.3); }}
        .invoice-button:hover {{ background: linear-gradient(135deg, #059669 0%, #047857 100%); }}
        .next-steps {{ background: #eff6ff; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #3b82f6; }}
        .next-steps h3 {{ margin-top: 0; color: #3b82f6; }}
        .next-steps ul {{ margin: 10px 0; padding-left: 20px; }}
        .next-steps li {{ margin: 8px 0; }}
        .footer {{ text-align: center; margin-top: 20px; padding: 20px; font-size: 12px; color: #666; }}
        .footer a {{ color: #10b981; text-decoration: none; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='checkmark'>✓</div>
            <h1>Paiement confirmé !</h1>
        </div>
        <div class='content'>
            <p>Bonjour <strong>{customerName}</strong>,</p>

            <p>Nous avons bien reçu votre paiement pour la commande <strong>{orderNumber}</strong>. Merci pour votre confiance !</p>

            <div class='order-details'>
                <h3>📋 Détails de la commande</h3>
                <div class='detail-row'>
                    <span class='detail-label'>Numéro de commande</span>
                    <span class='detail-value'>{orderNumber}</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Quantité de puces RFID</span>
                    <span class='detail-value'>{chipsQuantity} puces</span>
                </div>
                <div class='detail-row' style='border-bottom: none; margin-top: 10px;'>
                    <span class='detail-label'>Montant total TTC</span>
                    <span class='detail-value total'>{totalAmount:F2} €</span>
                </div>
            </div>

            <div class='invoice-section'>
                <h3 style='margin-top: 0; color: #059669;'>📄 Votre facture est prête</h3>
                <p style='color: #666; margin-bottom: 0;'>Cliquez sur le bouton ci-dessous pour télécharger votre facture au format PDF</p>
                <a href='{invoiceDownloadLink}' class='invoice-button'>📥 Télécharger la facture</a>
            </div>

            <div class='next-steps'>
                <h3>🚀 Prochaines étapes</h3>
                <ul>
                    <li><strong>Préparation</strong> : Votre commande va être préparée par nos équipes</li>
                    <li><strong>Notification</strong> : Vous recevrez un email avec le code PKG dès que votre commande sera prête à expédier</li>
                    <li><strong>Expédition</strong> : Une fois expédiée, vous recevrez le numéro de suivi de votre colis</li>
                </ul>
            </div>

            <p style='color: #666; font-size: 14px; margin-top: 20px;'>
                💡 <strong>Conseil</strong> : Conservez cette facture pour vos archives comptables. Vous pouvez également la retrouver à tout moment dans votre espace client.
            </p>

            <p style='margin-top: 30px;'>
                Si vous avez des questions concernant votre commande, n'hésitez pas à nous contacter.
            </p>

            <p>Cordialement,<br><strong>L'équipe Labor Control</strong></p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 Labor Control. Tous droits réservés.</p>
            <p>
                <a href='mailto:contact@labor-control.fr'>contact@labor-control.fr</a>
            </p>
        </div>
    </div>
</body>
</html>";

                var message = new CustomEmailMessage
                {
                    ToEmail = toEmail,
                    ToName = customerName,
                    Subject = $"✓ Paiement confirmé - Commande {orderNumber}",
                    HtmlContent = htmlContent
                };

                var result = await SendEmailAsync(message);

                if (result)
                {
                    _logger.LogInformation($"[PAYMENT CONFIRMATION] Email envoyé avec succès à {toEmail} pour la commande {orderNumber}");
                }
                else
                {
                    _logger.LogWarning($"[PAYMENT CONFIRMATION] Échec de l'envoi de l'email à {toEmail} pour la commande {orderNumber}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[PAYMENT CONFIRMATION] Erreur lors de l'envoi de l'email pour la commande {orderNumber}");
                return false;
            }
        }

        /// <summary>
        /// Envoie une invitation à un nouveau collaborateur staff avec lien pour définir son mot de passe
        /// </summary>
        public async Task<bool> SendStaffInvitationEmailAsync(string toEmail, string firstName, string lastName, string token)
        {
            try
            {
                // Récupérer l'URL de base du frontend staff
                var staffAppBaseUrl = Environment.GetEnvironmentVariable("STAFF_APP_URL")
                    ?? _configuration["STAFF_APP_URL"]
                    ?? "https://staff.labor-control.fr";

                var resetLink = $"{staffAppBaseUrl}/set-password?token={Uri.EscapeDataString(token)}";

                var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border: 1px solid #ddd; }}
        .welcome-box {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #667eea; }}
        .welcome-box h3 {{ margin-top: 0; color: #667eea; }}
        .activation-button {{ display: inline-block; padding: 15px 40px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 16px; margin: 20px 0; box-shadow: 0 4px 6px rgba(102, 126, 234, 0.3); }}
        .activation-button:hover {{ background: linear-gradient(135deg, #5568d3 0%, #653a8b 100%); }}
        .security-info {{ background: #fff8e1; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #ffc107; }}
        .password-rules {{ background: #e8f5e9; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #4caf50; }}
        .password-rules ul {{ margin: 10px 0; padding-left: 20px; }}
        .password-rules li {{ margin: 5px 0; }}
        .footer {{ text-align: center; margin-top: 20px; padding: 20px; font-size: 12px; color: #666; }}
        .footer a {{ color: #667eea; text-decoration: none; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Bienvenue chez Labor Control !</h1>
        </div>
        <div class='content'>
            <p>Bonjour <strong>{firstName} {lastName}</strong>,</p>

            <div class='welcome-box'>
                <h3>Votre compte collaborateur a été créé</h3>
                <p>Vous avez été ajouté à l'équipe Labor Control. Pour accéder à votre espace de travail, vous devez d'abord définir votre mot de passe.</p>
            </div>

            <div style='text-align: center; margin: 30px 0;'>
                <a href='{resetLink}' class='activation-button'>
                    Définir mon mot de passe
                </a>
            </div>

            <div class='password-rules'>
                <h4 style='margin-top: 0; color: #2e7d32;'>Règles de mot de passe</h4>
                <p style='margin-bottom: 10px;'>Votre mot de passe doit contenir :</p>
                <ul>
                    <li><strong>Minimum 8 caractères</strong></li>
                    <li><strong>Au moins 1 majuscule</strong> (A-Z)</li>
                    <li><strong>Au moins 1 chiffre</strong> (0-9)</li>
                    <li><strong>Au moins 1 caractère spécial</strong> (!@#$%^&*...)</li>
                </ul>
            </div>

            <div class='security-info'>
                <p style='margin: 0;'><strong>Sécurité :</strong> Ce lien est valable pendant <strong>24 heures</strong>. Si vous n'avez pas demandé la création de ce compte, veuillez ignorer cet email ou contacter votre administrateur.</p>
            </div>

            <p style='color: #666; font-size: 14px; margin-top: 20px;'>
                Si le bouton ne fonctionne pas, copiez et collez ce lien dans votre navigateur :<br>
                <a href='{resetLink}' style='color: #667eea; word-break: break-all;'>{resetLink}</a>
            </p>

            <p style='margin-top: 30px;'>
                À très bientôt !<br>
                <strong>L'équipe Labor Control</strong>
            </p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 Labor Control. Tous droits réservés.</p>
            <p>
                <a href='mailto:contact@labor-control.fr'>contact@labor-control.fr</a>
            </p>
        </div>
    </div>
</body>
</html>";

                var message = new CustomEmailMessage
                {
                    ToEmail = toEmail,
                    ToName = $"{firstName} {lastName}",
                    Subject = "Bienvenue chez Labor Control - Définissez votre mot de passe",
                    HtmlContent = htmlContent
                };

                var result = await SendEmailAsync(message);

                if (result)
                {
                    _logger.LogInformation($"[STAFF INVITATION] Email d'invitation envoyé avec succès à {toEmail}");
                }
                else
                {
                    _logger.LogWarning($"[STAFF INVITATION] Échec de l'envoi de l'email d'invitation à {toEmail}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[STAFF INVITATION] Erreur lors de l'envoi de l'email d'invitation à {toEmail}");
                return false;
            }
        }

        /// <summary>
        /// Envoie un email de bienvenue à un nouvel employé avec ses identifiants de première connexion
        /// </summary>
        public async Task<bool> SendNewEmployeeWelcomeEmailAsync(string toEmail, string firstName, string lastName, string username, string setupPin, DateTime pinExpiresAt)
        {
            try
            {
                var appUrl = Environment.GetEnvironmentVariable("AppUrl")
                    ?? _configuration["AppUrl"]
                    ?? "https://app.labor-control.fr";

                var firstLoginUrl = $"{appUrl}/first-login";
                var expirationDate = pinExpiresAt.ToString("dd/MM/yyyy");
                var expirationTime = pinExpiresAt.ToString("HH:mm");

                var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #0066cc; color: white; padding: 30px 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 30px 20px; border: 1px solid #ddd; }}
        .credentials-box {{ background-color: #e8f4fd; border-left: 4px solid #0066cc; padding: 20px; margin: 20px 0; border-radius: 4px; }}
        .pin-code {{ font-size: 32px; font-weight: bold; color: #0066cc; letter-spacing: 8px; text-align: center; padding: 15px; background-color: white; border-radius: 4px; margin: 10px 0; }}
        .username {{ font-size: 20px; font-weight: bold; color: #333; font-family: 'Courier New', monospace; text-align: center; padding: 10px; background-color: white; border-radius: 4px; margin: 10px 0; }}
        .warning-box {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 4px; }}
        .steps {{ background-color: white; padding: 20px; margin: 20px 0; border-radius: 4px; border: 1px solid #ddd; }}
        .steps ol {{ margin: 0; padding-left: 20px; }}
        .steps li {{ margin-bottom: 10px; }}
        .button {{ display: inline-block; padding: 15px 30px; background-color: #0066cc; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Bienvenue chez Labor Control !</h1>
        </div>
        <div class='content'>
            <p style='font-size: 16px;'><strong>Bonjour {firstName} {lastName},</strong></p>

            <p>Nous sommes ravis de vous accueillir en tant que nouvel utilisateur de <strong>Labor Control</strong>, la plateforme de gestion de maintenance industrielle de votre organisation.</p>

            <p>Votre compte a été créé avec succès. Voici vos identifiants de première connexion :</p>

            <div class='credentials-box'>
                <h3 style='margin-top: 0; color: #0066cc;'>📋 Vos identifiants</h3>

                <p><strong>Pseudo de connexion :</strong></p>
                <div class='username'>{username}</div>

                <p style='margin-top: 20px;'><strong>Code PIN de première connexion :</strong></p>
                <div class='pin-code'>{setupPin}</div>
            </div>

            <div class='warning-box'>
                <p style='margin: 0;'><strong>⚠️ Important :</strong> Votre code PIN expire le <strong>{expirationDate} à {expirationTime}</strong> (72 heures). Assurez-vous d'activer votre compte avant cette date.</p>
            </div>

            <div class='steps'>
                <h3 style='margin-top: 0; color: #333;'>📝 Comment activer votre compte ?</h3>
                <ol>
                    <li>Rendez-vous sur la page <strong>Première connexion</strong> de Labor Control</li>
                    <li>Entrez votre pseudo : <strong>{username}</strong></li>
                    <li>Entrez votre code PIN : <strong>{setupPin}</strong></li>
                    <li>Définissez votre mot de passe personnel (minimum 8 caractères)</li>
                    <li>Confirmez votre mot de passe</li>
                </ol>
                <p>Vous serez automatiquement connecté après avoir défini votre mot de passe.</p>
            </div>

            <div style='text-align: center;'>
                <a href='{firstLoginUrl}' class='button'>🔑 Activer mon compte maintenant</a>
            </div>

            <p style='margin-top: 30px;'>Une fois connecté, vous pourrez :</p>
            <ul>
                <li>Accéder à votre tableau de bord personnalisé</li>
                <li>Consulter et gérer vos tâches de maintenance</li>
                <li>Collaborer avec votre équipe</li>
                <li>Suivre l'avancement de vos interventions</li>
            </ul>

            <p style='margin-top: 30px;'>Si vous avez des questions ou besoin d'aide, n'hésitez pas à contacter votre administrateur.</p>

            <p style='margin-top: 20px;'>
                Excellente journée et bienvenue dans l'équipe !<br>
                <strong>L'équipe Labor Control</strong>
            </p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 Labor Control. Tous droits réservés.</p>
            <p>
                <a href='mailto:support@labor-control.fr' style='color: #0066cc; text-decoration: none;'>support@labor-control.fr</a>
            </p>
        </div>
    </div>
</body>
</html>";

                var message = new CustomEmailMessage
                {
                    ToEmail = toEmail,
                    ToName = $"{firstName} {lastName}",
                    Subject = "Bienvenue chez Labor Control - Activez votre compte",
                    HtmlContent = htmlContent
                };

                var result = await SendEmailAsync(message);

                if (result)
                {
                    _logger.LogInformation($"[NEW EMPLOYEE] Email de bienvenue envoyé avec succès à {toEmail} (Username: {username})");
                }
                else
                {
                    _logger.LogWarning($"[NEW EMPLOYEE] Échec de l'envoi de l'email de bienvenue à {toEmail}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[NEW EMPLOYEE] Erreur lors de l'envoi de l'email de bienvenue à {toEmail}");
                return false;
            }
        }

        /// <summary>
        /// Supprime les balises HTML d'une chaîne
        /// </summary>
        private string StripHtmlTags(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            var plainText = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", "");
            plainText = System.Net.WebUtility.HtmlDecode(plainText);
            return plainText;
        }
    }
}
