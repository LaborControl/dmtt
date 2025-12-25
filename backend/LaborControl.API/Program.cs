using LaborControl.API.Data;
using LaborControl.API.Services;
using LaborControl.API.Services.AI;
using LaborControl.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;

var builder = WebApplication.CreateBuilder(args);

// Configuration Azure Key Vault pour les secrets en production
if (!builder.Environment.IsDevelopment())
{
    var keyVaultName = builder.Configuration["KeyVaultName"] ?? "kv-lcdmtt";
    var keyVaultEndpoint = new Uri($"https://{keyVaultName}.vault.azure.net/");
    try
    {
        builder.Configuration.AddAzureKeyVault(
            keyVaultEndpoint,
            new DefaultAzureCredential());
        Console.WriteLine($"[STARTUP] Connected to Key Vault: {keyVaultName}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[STARTUP] Key Vault connection failed: {ex.Message}. Continuing with appsettings...");
    }
}

// Configuration de la base de données PostgreSQL avec pooling optimisé
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("[STARTUP] WARNING: No database connection string found!");
    connectionString = "Server=localhost;Database=laborcontrol;Port=5432;User Id=postgres;Password=postgres;";
}

// Ajouter les paramètres de pooling s'ils ne sont pas déjà présents
if (!connectionString.Contains("Pooling="))
{
    connectionString += ";Pooling=true;Minimum Pool Size=0;Maximum Pool Size=20;Connection Lifetime=300;Connection Idle Lifetime=60;";
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)
        .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));

// Configuration HttpClient pour SiretVerificationService
builder.Services.AddHttpClient<SiretVerificationService>();
builder.Services.AddScoped<SiretVerificationService>();

// Configuration StripePaymentService
builder.Services.AddScoped<StripePaymentService>();

// Configuration OrderFulfillmentService
builder.Services.AddScoped<OrderFulfillmentService>();

// Configuration InvoiceService
builder.Services.AddScoped<IInvoiceService, InvoiceService>();

// Configuration DeliveryNoteService (Bon de livraison)
builder.Services.AddScoped<IDeliveryNoteService, DeliveryNoteService>();

// Configuration RfidSecurityService (Anti-clonage RFID)
builder.Services.AddScoped<IRfidSecurityService, RfidSecurityService>();

// Configuration RfidReaderService (Lecteur RFID ACR122U)
builder.Services.AddScoped<IRfidReaderService, RfidReaderService>();

// Configuration EmailService (Azure Communication Services)
builder.Services.AddScoped<IEmailService, EmailService>();

// Configuration UsernameGenerator (Génération automatique de username)
builder.Services.AddScoped<IUsernameGenerator, UsernameGenerator>();

// Configuration StockManagementService (Gestion stock RFID)
builder.Services.AddScoped<StockManagementService>();

// Configuration BoxtalService (Intégration expédition Boxtal)
builder.Services.AddHttpClient(); // Pour IHttpClientFactory
builder.Services.AddScoped<IBoxtalService, BoxtalService>();

// Configuration FranceCompetencesService (Intégration RNCP/RS)
builder.Services.AddHttpClient<IFranceCompetencesService, FranceCompetencesService>();

// Configuration Services IA (DMTT - Claude + Gemini)
builder.Services.AddHttpClient<IAIService, ClaudeAIService>();
builder.Services.AddHttpClient<IGeminiOCRService, GeminiOCRService>();

// Configuration Background Service pour rappels et alertes
builder.Services.AddHostedService<TaskReminderAndAlertBackgroundService>();

// Configuration Rate Limiting (protection contre abus)
builder.Services.AddMemoryCache();
builder.Services.Configure<AspNetCoreRateLimit.IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.HttpStatusCode = 429; // Too Many Requests
    options.RealIpHeader = "X-Real-IP";
    options.ClientIdHeader = "X-ClientId";
    options.GeneralRules = new List<AspNetCoreRateLimit.RateLimitRule>
    {
        // Protection auth endpoints - prévenir brute force
        new AspNetCoreRateLimit.RateLimitRule
        {
            Endpoint = "POST:/api/auth/login",
            Period = "1m",
            Limit = 5 // 5 tentatives de connexion par minute
        },
        new AspNetCoreRateLimit.RateLimitRule
        {
            Endpoint = "POST:/api/auth/register-professional",
            Period = "1m",
            Limit = 2 // 2 inscriptions par minute (avec envoi email)
        },
        new AspNetCoreRateLimit.RateLimitRule
        {
            Endpoint = "POST:/api/auth/register-professional",
            Period = "1h",
            Limit = 10 // Max 10 inscriptions par heure
        },
        // Protection endpoints de commande
        new AspNetCoreRateLimit.RateLimitRule
        {
            Endpoint = "POST:/api/orders",
            Period = "1m",
            Limit = 10 // Max 10 commandes par minute
        },
        new AspNetCoreRateLimit.RateLimitRule
        {
            Endpoint = "POST:/api/orders/checkout",
            Period = "1m",
            Limit = 5 // Max 5 paiements Stripe par minute
        },
        // Protection générale pour tous les POST
        new AspNetCoreRateLimit.RateLimitRule
        {
            Endpoint = "POST:*",
            Period = "1m",
            Limit = 30 // Max 30 POST par minute sur tous les endpoints
        },
        // Protection générale pour tous les endpoints
        new AspNetCoreRateLimit.RateLimitRule
        {
            Endpoint = "*",
            Period = "1s",
            Limit = 10 // Max 10 requêtes par seconde
        }
    };
});
builder.Services.AddSingleton<AspNetCoreRateLimit.IIpPolicyStore, AspNetCoreRateLimit.MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<AspNetCoreRateLimit.IRateLimitCounterStore, AspNetCoreRateLimit.MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<AspNetCoreRateLimit.IRateLimitConfiguration, AspNetCoreRateLimit.RateLimitConfiguration>();
builder.Services.AddSingleton<AspNetCoreRateLimit.IProcessingStrategy, AspNetCoreRateLimit.AsyncKeyLockProcessingStrategy>();

// Configuration JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "VotreCleSecreteTresLonguePourLaborControl2025!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Ignorer les cycles de référence lors de la sérialisation JSON
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        // Encoder UTF-8 pour caractères accentués français et symboles spéciaux
        options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    });
// builder.Services.AddOpenApi(); // Commenté - pas disponible en .NET 8

// Configuration CORS pour permettre l'app web et mobile
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMobileApp",
        policy =>
        {
            policy.WithOrigins(
                "https://www.labor-control.fr",
                "https://labor-control.fr",
                "https://site.labor-control.fr",
                "https://gestion.labor-control.fr",
                "https://app.labor-control.fr",
                "https://red-glacier-020a78103.3.azurestaticapps.net",
                "https://yellow-rock-08b8d7903.3.azurestaticapps.net",
                "https://thankful-grass-0d1104e1e.3.azurestaticapps.net",
                "http://localhost:3000",
                "http://localhost:5000",
                "http://localhost:5093",
                "http://localhost:5140",
                "http://localhost:5141",
                "http://127.0.0.1:3000",
                "http://127.0.0.1:5000",
                "http://127.0.0.1:5093",
                "http://127.0.0.1:5140",
                "http://127.0.0.1:5141")
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials()
                   .WithExposedHeaders("Content-Disposition");
        });

    // Politique supplémentaire pour toutes les origines (fallback)
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .WithExposedHeaders("Content-Disposition");
        });
});

var app = builder.Build();

// Appliquer les migrations automatiquement au démarrage
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
        Console.WriteLine("[STARTUP] Migrations appliquées avec succès");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "[STARTUP] Erreur lors de l'application des migrations");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.MapOpenApi(); // Commenté - pas disponible en .NET 8
}

// IMPORTANT: CORS doit être AVANT HttpsRedirection et Rate Limiting
app.UseCors("AllowMobileApp");

app.UseHttpsRedirection();

// Rate Limiting - doit être avant l'authentification
app.UseMiddleware<AspNetCoreRateLimit.IpRateLimitMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

// Middleware de protection des routes staff
app.UseMiddleware<StaffAuthorizationMiddleware>();

app.MapControllers();

app.Run();
