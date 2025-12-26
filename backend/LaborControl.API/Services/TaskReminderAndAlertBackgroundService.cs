using LaborControl.API.Data;
using Microsoft.EntityFrameworkCore;

namespace LaborControl.API.Services
{
    /// <summary>
    /// Service de background qui envoie automatiquement :
    /// 1. Rappels de maintenance (J-1)
    /// 2. Alertes de retard pour tâches non complétées
    ///
    /// Gère les 3 types de tâches :
    /// - Type 1 : Tâches protocole (ControlPoint)
    /// - Type 2 : Maintenance préventive (Asset)
    /// - Type 3 : Maintenance curative (Asset)
    /// </summary>
    public class TaskReminderAndAlertBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TaskReminderAndAlertBackgroundService> _logger;
        private Timer? _timer;

        // Configuration (en minutes)
        private const int EXECUTION_INTERVAL_MINUTES = 60; // Exécute toutes les heures
        private const int REMINDER_HOURS_BEFORE = 24; // Rappel 24h avant

        public TaskReminderAndAlertBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<TaskReminderAndAlertBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[BACKGROUND] TaskReminderAndAlertBackgroundService démarré");

            // Exécuter immédiatement au démarrage
            await DoWork(stoppingToken);

            // Puis exécuter périodiquement
            _timer = new Timer(
                async _ => await DoWork(stoppingToken),
                null,
                TimeSpan.FromMinutes(EXECUTION_INTERVAL_MINUTES),
                TimeSpan.FromMinutes(EXECUTION_INTERVAL_MINUTES)
            );

            await Task.CompletedTask;
        }

        private async Task DoWork(CancellationToken stoppingToken)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    var now = DateTime.UtcNow;

                    // ========== RAPPELS (J-1) ==========
                    await SendReminders(context, emailService, now, stoppingToken);

                    // ========== ALERTES DE RETARD ==========
                    await SendOverdueAlerts(context, emailService, now, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BACKGROUND] Erreur dans TaskReminderAndAlertBackgroundService");
            }
        }

        /// <summary>
        /// Envoie des rappels pour les tâches prévues dans les prochaines 24h
        /// </summary>
        private async Task SendReminders(
            ApplicationDbContext context,
            IEmailService emailService,
            DateTime now,
            CancellationToken stoppingToken)
        {
            try
            {
                var reminderStart = now;
                var reminderEnd = now.AddHours(REMINDER_HOURS_BEFORE);

                // Récupérer les tâches à rappeler
                var tasksToRemind = await context.ScheduledTasks
                    .Include(t => t.User)
                    .Include(t => t.ControlPoint)
                    .Include(t => t.Asset)
                    .Include(t => t.MaintenanceSchedule)
                    .Where(t =>
                        t.Status == "PENDING" &&
                        t.ScheduledDate >= reminderStart &&
                        t.ScheduledDate <= reminderEnd &&
                        t.User != null &&
                        !string.IsNullOrEmpty(t.User.Email)
                    )
                    .ToListAsync(stoppingToken);

                _logger.LogInformation($"[BACKGROUND] {tasksToRemind.Count} tâche(s) à rappeler");

                foreach (var task in tasksToRemind)
                {
                    try
                    {
                        // Déterminer le type et le nom de la tâche
                        string taskName = task.TaskType == "PROTOCOL"
                            ? task.ControlPoint?.Name ?? "Tâche protocole"
                            : task.Asset?.Name ?? "Maintenance";

                        string description = task.TaskType == "PROTOCOL"
                            ? $"Tâche protocole au point de contrôle {task.ControlPoint?.Name}"
                            : $"Maintenance {task.MaintenanceSchedule?.Type ?? "préventive"} sur {task.Asset?.Name}";

                        // Calculer les jours jusqu'à l'échéance
                        var daysUntilDue = (int)(task.ScheduledDate - now).TotalDays;

                        // Envoyer le rappel
                        var emailSent = await emailService.SendMaintenanceReminderEmailAsync(
                            task.User!.Email!,
                            task.User!.Prenom!,
                            taskName,
                            task.ScheduledDate,
                            daysUntilDue
                        );

                        if (emailSent)
                        {
                            _logger.LogInformation(
                                $"[EMAIL] Rappel envoyé à {task.User.Email} pour tâche {task.Id} ({taskName})");
                        }
                        else
                        {
                            _logger.LogWarning(
                                $"[EMAIL] Échec de l'envoi du rappel à {task.User.Email}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            $"[EMAIL] Erreur lors de l'envoi du rappel pour tâche {task.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BACKGROUND] Erreur lors de l'envoi des rappels");
            }
        }

        /// <summary>
        /// Envoie des alertes pour les tâches en retard
        /// </summary>
        private async Task SendOverdueAlerts(
            ApplicationDbContext context,
            IEmailService emailService,
            DateTime now,
            CancellationToken stoppingToken)
        {
            try
            {
                // Récupérer les tâches en retard
                var overdueTasks = await context.ScheduledTasks
                    .Include(t => t.User)
                    .Include(t => t.ControlPoint)
                    .Include(t => t.Asset)
                    .Include(t => t.MaintenanceSchedule)
                    .Include(t => t.Customer)
                    .Where(t =>
                        t.Status == "PENDING" &&
                        t.ScheduledDate < now &&
                        t.User != null &&
                        !string.IsNullOrEmpty(t.User.Email)
                    )
                    .ToListAsync(stoppingToken);

                _logger.LogInformation($"[BACKGROUND] {overdueTasks.Count} tâche(s) en retard");

                foreach (var task in overdueTasks)
                {
                    try
                    {
                        // Vérifier si la tâche dépasse la tolérance de retard
                        var delayMinutes = (int)(now - task.ScheduledDate).TotalMinutes;
                        var toleranceMinutes = task.DelayToleranceUnit switch
                        {
                            "MINUTES" => task.DelayToleranceValue,
                            "HOURS" => task.DelayToleranceValue * 60,
                            "DAYS" => task.DelayToleranceValue * 24 * 60,
                            _ => 0
                        };

                        // Envoyer alerte seulement si retard > tolérance
                        if (delayMinutes > toleranceMinutes)
                        {
                            // Déterminer le type et le nom de la tâche
                            string taskName = task.TaskType == "PROTOCOL"
                                ? task.ControlPoint?.Name ?? "Tâche protocole"
                                : task.Asset?.Name ?? "Maintenance";

                            string description = task.TaskType == "PROTOCOL"
                                ? $"Tâche protocole au point de contrôle {task.ControlPoint?.Name}"
                                : $"Maintenance {task.MaintenanceSchedule?.Type ?? "préventive"} sur {task.Asset?.Name}";

                        // Calculer les jours de retard
                            var daysOverdue = (int)Math.Ceiling(delayMinutes / (24.0 * 60));

                            // Envoyer l'alerte
                            var emailSent = await emailService.SendMaintenanceOverdueAlertEmailAsync(
                                task.User!.Email!,
                                task.User!.Prenom!,
                                taskName,
                                daysOverdue
                            );

                            if (emailSent)
                            {
                                _logger.LogInformation(
                                    $"[EMAIL] Alerte retard envoyée à {task.User.Email} pour tâche {task.Id} ({taskName}) - Retard: {delayMinutes}min");
                            }
                            else
                            {
                                _logger.LogWarning(
                                    $"[EMAIL] Échec de l'envoi de l'alerte retard à {task.User.Email}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            $"[EMAIL] Erreur lors de l'envoi de l'alerte retard pour tâche {task.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BACKGROUND] Erreur lors de l'envoi des alertes de retard");
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[BACKGROUND] TaskReminderAndAlertBackgroundService arrêté");
            _timer?.Dispose();
            await base.StopAsync(stoppingToken);
        }
    }
}
