using System.Security.Claims;

namespace LaborControl.API.Middleware
{
    /// <summary>
    /// Middleware pour protéger les routes /admin-lc/*
    /// Vérifie que seul le staff Labor Control peut y accéder
    /// </summary>
    public class StaffAuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<StaffAuthorizationMiddleware> _logger;

        public StaffAuthorizationMiddleware(RequestDelegate next, ILogger<StaffAuthorizationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Vérifier si la route est protégée (/admin-lc/*)
            if (context.Request.Path.StartsWithSegments("/admin-lc"))
            {
                // Vérifier si l'utilisateur est authentifié
                if (!context.User.Identity?.IsAuthenticated ?? true)
                {
                    _logger.LogWarning($"[STAFF AUTH] Accès non authentifié à {context.Request.Path}");
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { message = "Authentification requise" });
                    return;
                }

                // Vérifier que c'est un utilisateur STAFF (pas CLIENT)
                var userType = context.User.FindFirst("UserType")?.Value;

                if (userType != "STAFF")
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    _logger.LogWarning($"[STAFF AUTH] Tentative d'accès non-staff à {context.Request.Path} (UserId: {userId}, UserType: {userType})");
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new { message = "Accès réservé au staff Labor Control" });
                    return;
                }

                // Vérifier que le staff est actif
                var staffRole = context.User.FindFirst("StaffRole")?.Value;
                if (string.IsNullOrEmpty(staffRole))
                {
                    _logger.LogWarning($"[STAFF AUTH] Staff sans rôle détecté");
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new { message = "Rôle staff invalide" });
                    return;
                }

                _logger.LogInformation($"[STAFF AUTH] Accès autorisé à {context.Request.Path} (Rôle: {staffRole})");
            }

            await _next(context);
        }
    }
}
