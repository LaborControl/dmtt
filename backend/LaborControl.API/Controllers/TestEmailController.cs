using LaborControl.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaborControl.API.Controllers
{
    /// <summary>
    /// Contrôleur de test pour le service d'email
    /// ATTENTION: Ces endpoints sont UNIQUEMENT pour le développement/test
    /// En production, ils doivent être protégés ou désactivés
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Protéger tous les endpoints par défaut
    public class TestEmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<TestEmailController> _logger;
        private readonly IConfiguration _configuration;

        public TestEmailController(IEmailService emailService, ILogger<TestEmailController> logger, IConfiguration configuration)
        {
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Vérifie la configuration du service email
        /// Requiert authentification Admin
        /// </summary>
        [HttpGet("check-config")]
        public IActionResult CheckEmailConfig()
        {
            var connectionString = _configuration["AzureCommunicationServices__ConnectionString"];
            var senderEmail = _configuration["AzureCommunicationServices__SenderEmail"];

            // Vérifier aussi les variables d'environnement directement
            var envConnectionString = Environment.GetEnvironmentVariable("AzureCommunicationServices__ConnectionString");
            var envSenderEmail = Environment.GetEnvironmentVariable("AzureCommunicationServices__SenderEmail");

            return Ok(new
            {
                hasConnectionString = !string.IsNullOrEmpty(connectionString),
                connectionStringLength = connectionString?.Length ?? 0,
                hasSenderEmail = !string.IsNullOrEmpty(senderEmail),
                senderEmail = senderEmail ?? "NOT_CONFIGURED",
                envHasConnectionString = !string.IsNullOrEmpty(envConnectionString),
                envConnectionStringLength = envConnectionString?.Length ?? 0,
                envHasSenderEmail = !string.IsNullOrEmpty(envSenderEmail),
                envSenderEmail = envSenderEmail ?? "NOT_CONFIGURED",
                aspnetcoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "NOT_SET"
            });
        }

        /// <summary>
        /// Vérifie si EmailService est initialisé
        /// Requiert authentification Admin
        /// </summary>
        [HttpGet("service-status")]
        public IActionResult GetServiceStatus()
        {
            // Utiliser réflexion pour vérifier si _emailClient est null
            var emailServiceType = _emailService.GetType();
            var emailClientField = emailServiceType.GetField("_emailClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var senderEmailField = emailServiceType.GetField("_senderEmail", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var emailClient = emailClientField?.GetValue(_emailService);
            var senderEmail = senderEmailField?.GetValue(_emailService);

            return Ok(new
            {
                isInitialized = emailClient != null,
                senderEmail = senderEmail?.ToString() ?? "NOT_SET"
            });
        }

        /// <summary>
        /// Envoie un email de test de bienvenue
        /// Requiert authentification Admin
        /// </summary>
        [HttpPost("send-welcome")]
        public async Task<IActionResult> SendWelcomeEmail([FromQuery] string email, [FromQuery] string name)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(name))
            {
                return BadRequest(new { error = "Email et name sont requis" });
            }

            try
            {
                var result = await _emailService.SendWelcomeEmailAsync(email, name);
                if (result)
                {
                    return Ok(new { message = "Email de bienvenue envoyé avec succès", email });
                }
                else
                {
                    return StatusCode(500, new { error = "Erreur lors de l'envoi de l'email" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de l'email de test");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Envoie un email de test d'assignation de tâche
        /// Requiert authentification Admin
        /// </summary>
        [HttpPost("send-task-assignment")]
        public async Task<IActionResult> SendTaskAssignmentEmail(
            [FromQuery] string email,
            [FromQuery] string technicianName,
            [FromQuery] string taskName,
            [FromQuery] string taskDescription,
            [FromQuery] DateTime dueDate)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(technicianName) ||
                string.IsNullOrEmpty(taskName) || string.IsNullOrEmpty(taskDescription))
            {
                return BadRequest(new { error = "Tous les paramètres sont requis" });
            }

            try
            {
                var result = await _emailService.SendTaskAssignmentEmailAsync(
                    email, technicianName, taskName, taskDescription, dueDate);

                if (result)
                {
                    return Ok(new { message = "Email d'assignation de tâche envoyé avec succès", email });
                }
                else
                {
                    return StatusCode(500, new { error = "Erreur lors de l'envoi de l'email" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de l'email de test");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Envoie un email de test de rappel de maintenance
        /// Requiert authentification Admin
        /// </summary>
        [HttpPost("send-maintenance-reminder")]
        public async Task<IActionResult> SendMaintenanceReminderEmail(
            [FromQuery] string email,
            [FromQuery] string technicianName,
            [FromQuery] string maintenanceName,
            [FromQuery] DateTime dueDate,
            [FromQuery] int daysUntilDue)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(technicianName) ||
                string.IsNullOrEmpty(maintenanceName))
            {
                return BadRequest(new { error = "Tous les paramètres sont requis" });
            }

            try
            {
                var result = await _emailService.SendMaintenanceReminderEmailAsync(
                    email, technicianName, maintenanceName, dueDate, daysUntilDue);

                if (result)
                {
                    return Ok(new { message = "Email de rappel de maintenance envoyé avec succès", email });
                }
                else
                {
                    return StatusCode(500, new { error = "Erreur lors de l'envoi de l'email" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de l'email de test");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Envoie un email de test d'alerte de maintenance en retard
        /// Requiert authentification Admin
        /// </summary>
        [HttpPost("send-maintenance-overdue-alert")]
        public async Task<IActionResult> SendMaintenanceOverdueAlertEmail(
            [FromQuery] string email,
            [FromQuery] string supervisorName,
            [FromQuery] string maintenanceName,
            [FromQuery] int daysOverdue)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(supervisorName) ||
                string.IsNullOrEmpty(maintenanceName))
            {
                return BadRequest(new { error = "Tous les paramètres sont requis" });
            }

            try
            {
                var result = await _emailService.SendMaintenanceOverdueAlertEmailAsync(
                    email, supervisorName, maintenanceName, daysOverdue);

                if (result)
                {
                    return Ok(new { message = "Email d'alerte de maintenance envoyé avec succès", email });
                }
                else
                {
                    return StatusCode(500, new { error = "Erreur lors de l'envoi de l'email" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de l'email de test");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Envoie un email de test de confirmation de commande
        /// Requiert authentification Admin
        /// </summary>
        [HttpPost("send-order-confirmation")]
        public async Task<IActionResult> SendOrderConfirmationEmail(
            [FromQuery] string email,
            [FromQuery] string customerName,
            [FromQuery] string orderNumber,
            [FromQuery] decimal totalAmount)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(customerName) ||
                string.IsNullOrEmpty(orderNumber))
            {
                return BadRequest(new { error = "Tous les paramètres sont requis" });
            }

            try
            {
                var result = await _emailService.SendOrderConfirmationEmailAsync(
                    email, customerName, orderNumber, totalAmount);

                if (result)
                {
                    return Ok(new { message = "Email de confirmation de commande envoyé avec succès", email });
                }
                else
                {
                    return StatusCode(500, new { error = "Erreur lors de l'envoi de l'email" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de l'email de test");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
