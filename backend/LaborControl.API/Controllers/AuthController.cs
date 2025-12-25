using LaborControl.API.Data;
using LaborControl.API.DTOs;
using LaborControl.API.Services;
using LaborControl.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LaborControl.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly SiretVerificationService _siretService;
        private readonly IEmailService _emailService;
        private readonly IUsernameGenerator _usernameGenerator;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ApplicationDbContext context,
            IConfiguration configuration,
            SiretVerificationService siretService,
            IEmailService emailService,
            IUsernameGenerator usernameGenerator,
            ILogger<AuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _siretService = siretService;
            _emailService = emailService;
            _usernameGenerator = usernameGenerator;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
        {
            // Vérifier l'utilisateur par email OU username (insensible à la casse)
            // Optimisation: AsNoTracking pour lecture seule + pas d'Include inutile
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == request.Email || (u.Username != null && u.Username.ToLower() == request.Email.ToLower()));

            if (user == null)
            {
                return Unauthorized(new { message = "Identifiant ou mot de passe incorrect" });
            }

            // SÉCURITÉ: Vérifier mot de passe avec migration automatique des mots de passe en clair
            bool passwordValid = false;
            bool needsMigration = false;

            // Détecter si c'est un ancien mot de passe en clair (longueur < 30 caractères = pas BCrypt)
            // BCrypt produit toujours des hash de 60 caractères
            if (!string.IsNullOrEmpty(user.PasswordHash) && user.PasswordHash.Length < 30)
            {
                // Vérifier mot de passe en clair (migration legacy)
                if (user.PasswordHash == request.Password)
                {
                    passwordValid = true;
                    needsMigration = true;
                    _logger.LogWarning($"[SECURITY] Mot de passe en clair détecté pour {user.Email} - migration nécessaire");
                }
            }
            else
            {
                // Vérifier mot de passe BCrypt (normal)
                try
                {
                    passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[LOGIN] Erreur BCrypt.Verify pour {user.Email}");
                    return Unauthorized(new { message = "Identifiant ou mot de passe incorrect" });
                }
            }

            if (!passwordValid)
            {
                return Unauthorized(new { message = "Identifiant ou mot de passe incorrect" });
            }

            // Migration automatique du mot de passe en clair vers BCrypt
            if (needsMigration)
            {
                try
                {
                    // Re-charger l'utilisateur AVEC tracking pour pouvoir le mettre à jour
                    var userToUpdate = await _context.Users.FindAsync(user.Id);
                    if (userToUpdate != null)
                    {
                        userToUpdate.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"[SECURITY] ✅ Mot de passe migré vers BCrypt pour {user.Email}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[SECURITY] ❌ Échec migration mot de passe pour {user.Email}");
                    // Ne pas bloquer le login si la migration échoue
                }
            }

            // Générer le token JWT
            var token = GenerateJwtToken(user);

            return Ok(new LoginResponse
            {
                Token = token,
                RequiresPasswordChange = user.RequiresPasswordChange,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Username = user.Username,
                    Nom = user.Nom,
                    Prenom = user.Prenom,
                    Niveau = user.Niveau,
                    Role = user.Role,
                    CustomerId = user.CustomerId
                }
            });
        }

        private string GenerateJwtToken(LaborControl.API.Models.User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? "VotreCleSecreteTresLonguePourLaborControl2025!"));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("CustomerId", user.CustomerId.ToString()),
                new Claim("UserType", "CLIENT")  // Important: distingue clients des staff
            };

            // Ajouter le claim Email seulement si l'email n'est pas vide
            // (certains utilisateurs créés avec Username/PIN n'ont pas d'email)
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
            }

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "LaborControl",
                audience: _configuration["Jwt:Audience"] ?? "LaborControlApp",
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Normalise un numéro de téléphone au format international E.164
        /// Ajoute automatiquement +33 (France) si le préfixe est manquant
        /// </summary>
        private string NormalizePhoneNumber(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return string.Empty;

            // Nettoyer : supprimer espaces, tirets, points
            var cleaned = phone.Trim()
                .Replace(" ", "")
                .Replace("-", "")
                .Replace(".", "");

            // Si déjà avec préfixe international, retourner tel quel
            if (cleaned.StartsWith("+"))
                return cleaned;

            // Si commence par 0, le supprimer et ajouter +33 (France)
            if (cleaned.StartsWith("0"))
                return "+33" + cleaned.Substring(1);

            // Sinon, ajouter +33 directement
            return "+33" + cleaned;
        }

        [HttpPost("register-professional")]
        public async Task<ActionResult<RegisterProfessionalResponse>> RegisterProfessional(RegisterProfessionalRequest request)
        {
            // Vérifier si l'email existe déjà
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
            {
                return BadRequest(new RegisterProfessionalResponse
                {
                    Success = false,
                    Message = "Un compte existe déjà avec cet email"
                });
            }

            // Vérifier si le SIRET existe déjà
            var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.Siret == request.Siret);
            if (existingCustomer != null)
            {
                return BadRequest(new RegisterProfessionalResponse
                {
                    Success = false,
                    Message = "Un compte existe déjà pour cette entreprise (SIRET)"
                });
            }

            // Vérifier le SIRET avec l'API INSEE
            var siretValidation = await _siretService.VerifySiretAsync(request.Siret);

            if (!siretValidation.IsValid)
            {
                return BadRequest(new RegisterProfessionalResponse
                {
                    Success = false,
                    Message = siretValidation.ErrorMessage ?? "SIRET invalide"
                });
            }

            if (!siretValidation.IsActive)
            {
                return BadRequest(new RegisterProfessionalResponse
                {
                    Success = false,
                    Message = "Cette entreprise est fermée administrativement"
                });
            }

            // Normaliser le numéro de téléphone
            var normalizedPhone = NormalizePhoneNumber(request.Phone);

            // Créer le Customer
            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                Name = request.CompanyName,
                Siret = request.Siret,
                ApeCode = siretValidation.ActivityCode,
                Address = $"{request.Address}, {request.PostalCode} {request.City}",
                Website = request.Website,
                ContactName = $"{request.FirstName} {request.LastName}",
                ContactEmail = request.Email,
                ContactPhone = normalizedPhone,
                SubscriptionPlan = "trial",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Customers.Add(customer);

            // Générer le username automatiquement (format: PrénomNNN)
            var generatedUsername = await _usernameGenerator.GenerateUniqueUsernameAsync(
                request.FirstName,
                request.LastName,
                customer.Id);

            _logger.LogInformation($"[INSCRIPTION] Username généré pour {request.Email}: {generatedUsername}");

            // Créer l'utilisateur admin pour ce customer
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                Username = generatedUsername,
                Nom = request.LastName,
                Prenom = request.FirstName,
                Tel = normalizedPhone,
                Service = "Direction",
                Fonction = request.JobTitle,
                Niveau = "Admin",
                Role = "ADMIN",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CustomerId = customer.Id,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                JobTitle = request.JobTitle,
                CanApproveDeviations = true,
                IsAccountOwner = true  // Contact principal qui a créé le compte
            };

            _context.Users.Add(user);

            // Créer le site par défaut "Site principal"
            var defaultSite = new Site
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                Name = "Site principal",
                Address = $"{request.Address}",
                City = request.City,
                PostalCode = request.PostalCode,
                Country = "France",
                ContactName = $"{request.FirstName} {request.LastName}",
                ContactPhone = normalizedPhone,
                ContactEmail = request.Email,
                Siret = request.Siret,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Sites.Add(defaultSite);

            _logger.LogInformation($"[INSCRIPTION] Création du site par défaut pour le client {customer.Name} (CustomerId: {customer.Id}, Email: {request.Email}, SiteId: {defaultSite.Id})");

            // Sauvegarder
            await _context.SaveChangesAsync();

            _logger.LogInformation($"[INSCRIPTION] Site par défaut créé avec succès pour {request.Email}");

            // Initialiser les catégories et types d'équipements prédéfinis
            try
            {
                _logger.LogInformation($"[INSCRIPTION] Initialisation des catégories d'équipements pour {customer.Name}");

                var categories = new List<EquipmentCategory>
                {
                    new EquipmentCategory { Id = Guid.NewGuid(), CustomerId = customer.Id, Name = "Production", Code = "PROD", Description = "Équipements de production", Color = "#3B82F6", Icon = "🏭", DisplayOrder = 1, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentCategory { Id = Guid.NewGuid(), CustomerId = customer.Id, Name = "Utilités", Code = "UTIL", Description = "Équipements utilitaires", Color = "#F59E0B", Icon = "⚡", DisplayOrder = 2, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentCategory { Id = Guid.NewGuid(), CustomerId = customer.Id, Name = "Sécurité", Code = "SECU", Description = "Équipements de sécurité", Color = "#EF4444", Icon = "🛡️", DisplayOrder = 3, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentCategory { Id = Guid.NewGuid(), CustomerId = customer.Id, Name = "Infrastructure", Code = "INFRA", Description = "Équipements d'infrastructure", Color = "#6366F1", Icon = "🏢", DisplayOrder = 4, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow }
                };

                _context.EquipmentCategories.AddRange(categories);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"[INSCRIPTION] {categories.Count} catégories d'équipements créées pour {customer.Name}");

                // Créer les 103 types d'équipements prédéfinis
                var prodCat = categories.First(c => c.Code == "PROD");
                var utilCat = categories.First(c => c.Code == "UTIL");
                var secuCat = categories.First(c => c.Code == "SECU");
                var infraCat = categories.First(c => c.Code == "INFRA");

                var types = new List<EquipmentType>();

                // Production (28 types)
                types.AddRange(new[] {
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Tour", Code = "LATHE", Icon = "🔧", DisplayOrder = 1, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Fraiseuse", Code = "MILLING_MACHINE", Icon = "⚙️", DisplayOrder = 2, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Perceuse", Code = "DRILL", Icon = "🔩", DisplayOrder = 3, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Presse", Code = "PRESS", Icon = "🔨", DisplayOrder = 4, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Rectifieuse", Code = "GRINDER", Icon = "⚙️", DisplayOrder = 5, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Centre d'usinage", Code = "MACHINING_CENTER", Icon = "🏭", DisplayOrder = 6, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Réacteur", Code = "REACTOR", Icon = "⚗️", DisplayOrder = 10, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Échangeur thermique", Code = "HEAT_EXCHANGER", Icon = "🔄", DisplayOrder = 11, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Colonne de distillation", Code = "DISTILLATION_COLUMN", Icon = "🏗️", DisplayOrder = 12, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Séparateur", Code = "SEPARATOR", Icon = "↔️", DisplayOrder = 13, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Mélangeur", Code = "MIXER", Icon = "🌀", DisplayOrder = 14, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Broyeur", Code = "CRUSHER", Icon = "⚙️", DisplayOrder = 15, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Filtre", Code = "FILTER", Icon = "🔬", DisplayOrder = 16, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Pompe centrifuge", Code = "CENTRIFUGAL_PUMP", Icon = "💧", DisplayOrder = 20, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Pompe volumétrique", Code = "VOLUMETRIC_PUMP", Icon = "💦", DisplayOrder = 21, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Compresseur", Code = "COMPRESSOR", Icon = "🌀", DisplayOrder = 22, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Vanne de régulation", Code = "CONTROL_VALVE", Icon = "🔧", DisplayOrder = 23, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Four industriel", Code = "INDUSTRIAL_FURNACE", Icon = "🔥", DisplayOrder = 30, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Sécheur", Code = "DRYER", Icon = "🌡️", DisplayOrder = 31, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Refroidisseur", Code = "COOLER", Icon = "❄️", DisplayOrder = 32, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Convoyeur à bande", Code = "BELT_CONVEYOR", Icon = "➡️", DisplayOrder = 40, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Convoyeur à rouleaux", Code = "ROLLER_CONVEYOR", Icon = "🔄", DisplayOrder = 41, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Élévateur", Code = "ELEVATOR_CONVEYOR", Icon = "⬆️", DisplayOrder = 42, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Pont roulant", Code = "OVERHEAD_CRANE", Icon = "🏗️", DisplayOrder = 43, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Chariot élévateur", Code = "FORKLIFT", Icon = "🚜", DisplayOrder = 44, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Robot industriel", Code = "INDUSTRIAL_ROBOT", Icon = "🤖", DisplayOrder = 50, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Automate programmable", Code = "PLC", Icon = "💻", DisplayOrder = 51, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = prodCat.Id, Name = "Système de vision", Code = "VISION_SYSTEM", Icon = "👁️", DisplayOrder = 52, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow }
                });

                // Utilités (20 types)
                types.AddRange(new[] {
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = utilCat.Id, Name = "Chaudière vapeur", Code = "STEAM_BOILER", Icon = "🔥", DisplayOrder = 1, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = utilCat.Id, Name = "Chaudière eau chaude", Code = "HOT_WATER_BOILER", Icon = "♨️", DisplayOrder = 2, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = utilCat.Id, Name = "Groupe électrogène", Code = "GENERATOR", Icon = "⚡", DisplayOrder = 3, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = utilCat.Id, Name = "Transformateur électrique", Code = "TRANSFORMER", Icon = "🔌", DisplayOrder = 4, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = utilCat.Id, Name = "Onduleur", Code = "UPS", Icon = "🔋", DisplayOrder = 5, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = utilCat.Id, Name = "Armoire électrique", Code = "ELECTRICAL_CABINET", Icon = "⚡", DisplayOrder = 6, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = utilCat.Id, Name = "Groupe froid", Code = "CHILLER", Icon = "❄️", DisplayOrder = 10, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = utilCat.Id, Name = "Tour de refroidissement", Code = "COOLING_TOWER", Icon = "🌡️", DisplayOrder = 11, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = utilCat.Id, Name = "Centrale de traitement d'air", Code = "AHU", Icon = "🌬️", DisplayOrder = 12, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = utilCat.Id, Name = "Climatiseur", Code = "AIR_CONDITIONER", Icon = "❄️", DisplayOrder = 13, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = utilCat.Id, Name = "Ventilateur", Code = "FAN", Icon = "💨", DisplayOrder = 14, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = utilCat.Id, Name = "Compresseur d'air", Code = "AIR_COMPRESSOR", Icon = "🌀", DisplayOrder = 20, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = utilCat.Id, Name = "Sécheur d'air", Code = "AIR_DRYER", Icon = "💨", DisplayOrder = 21, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = utilCat.Id, Name = "Réservoir d'air", Code = "AIR_RECEIVER", Icon = "🛢️", DisplayOrder = 22, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = utilCat.Id, Name = "Adoucisseur", Code = "WATER_SOFTENER", Icon = "💧", DisplayOrder = 30, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = utilCat.Id, Name = "Osmoseur", Code = "REVERSE_OSMOSIS", Icon = "🔬", DisplayOrder = 31, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = utilCat.Id, Name = "Station d'épuration", Code = "WASTEWATER_TREATMENT", Icon = "♻️", DisplayOrder = 32, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = utilCat.Id, Name = "Dégraisseur", Code = "GREASE_SEPARATOR", Icon = "🛢️", DisplayOrder = 33, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = utilCat.Id, Name = "Surpresseur", Code = "BOOSTER_PUMP", Icon = "💧", DisplayOrder = 34, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow }
                });

                // Sécurité (24 types)
                types.AddRange(new[] {
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = secuCat.Id, Name = "Détecteur de fumée", Code = "SMOKE_DETECTOR", Icon = "🚨", DisplayOrder = 1, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = secuCat.Id, Name = "Détecteur de chaleur", Code = "HEAT_DETECTOR", Icon = "🔥", DisplayOrder = 2, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = secuCat.Id, Name = "Extincteur", Code = "EXTINGUISHER", Icon = "🧯", DisplayOrder = 3, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = secuCat.Id, Name = "RIA (Robinet Incendie Armé)", Code = "FIRE_HOSE_REEL", Icon = "🚒", DisplayOrder = 4, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = secuCat.Id, Name = "Sprinkler", Code = "SPRINKLER", Icon = "💦", DisplayOrder = 5, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = secuCat.Id, Name = "Centrale incendie", Code = "FIRE_ALARM_PANEL", Icon = "🔴", DisplayOrder = 6, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = secuCat.Id, Name = "Déclencheur manuel", Code = "MANUAL_CALL_POINT", Icon = "🔴", DisplayOrder = 7, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = secuCat.Id, Name = "Caméra de surveillance", Code = "CCTV_CAMERA", Icon = "📹", DisplayOrder = 10, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = secuCat.Id, Name = "Lecteur de badge", Code = "CARD_READER", Icon = "💳", DisplayOrder = 11, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = secuCat.Id, Name = "Portique de sécurité", Code = "SECURITY_GATE", Icon = "🚧", DisplayOrder = 12, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = secuCat.Id, Name = "Tourniquet", Code = "TURNSTILE", Icon = "🚪", DisplayOrder = 13, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = secuCat.Id, Name = "Interphone", Code = "INTERCOM", Icon = "📞", DisplayOrder = 14, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = secuCat.Id, Name = "Alarme intrusion", Code = "INTRUSION_ALARM", Icon = "🔔", DisplayOrder = 20, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = secuCat.Id, Name = "Détecteur de mouvement", Code = "MOTION_DETECTOR", Icon = "👁️", DisplayOrder = 21, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = secuCat.Id, Name = "Détecteur de gaz", Code = "GAS_DETECTOR", Icon = "⚠️", DisplayOrder = 22, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = secuCat.Id, Name = "Sirène d'alarme", Code = "ALARM_SIREN", Icon = "🔊", DisplayOrder = 23, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = secuCat.Id, Name = "BAES (Bloc Autonome)", Code = "EMERGENCY_LIGHT", Icon = "💡", DisplayOrder = 30, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = secuCat.Id, Name = "Éclairage de sécurité", Code = "SAFETY_LIGHTING", Icon = "🔦", DisplayOrder = 31, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = secuCat.Id, Name = "Douche de sécurité", Code = "SAFETY_SHOWER", Icon = "🚿", DisplayOrder = 40, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = secuCat.Id, Name = "Lave-œil", Code = "EYE_WASH", Icon = "👁️", DisplayOrder = 41, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = secuCat.Id, Name = "Défibrillateur", Code = "DEFIBRILLATOR", Icon = "❤️", DisplayOrder = 42, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = secuCat.Id, Name = "Armoire à pharmacie", Code = "FIRST_AID_KIT", Icon = "🏥", DisplayOrder = 43, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow }
                });

                // Infrastructure (31 types)
                types.AddRange(new[] {
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Bâtiment", Code = "BUILDING", Icon = "🏢", DisplayOrder = 1, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Local technique", Code = "TECH_ROOM", Icon = "🔧", DisplayOrder = 2, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Atelier", Code = "WORKSHOP", Icon = "🏭", DisplayOrder = 3, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Entrepôt", Code = "WAREHOUSE", Icon = "📦", DisplayOrder = 4, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Bureau", Code = "OFFICE", Icon = "🏢", DisplayOrder = 5, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Ascenseur", Code = "ELEVATOR", Icon = "🛗", DisplayOrder = 10, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Monte-charge", Code = "GOODS_LIFT", Icon = "📦", DisplayOrder = 11, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Escalier mécanique", Code = "ESCALATOR", Icon = "🚶", DisplayOrder = 12, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Tapis roulant", Code = "MOVING_WALKWAY", Icon = "➡️", DisplayOrder = 13, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Porte automatique", Code = "AUTO_DOOR", Icon = "🚪", DisplayOrder = 20, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Porte sectionnelle", Code = "SECTIONAL_DOOR", Icon = "🚪", DisplayOrder = 21, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Rideau métallique", Code = "ROLLER_SHUTTER", Icon = "🪟", DisplayOrder = 22, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Portail", Code = "GATE", Icon = "🚧", DisplayOrder = 23, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Barrière levante", Code = "BARRIER_GATE", Icon = "🚧", DisplayOrder = 24, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Quai de chargement", Code = "LOADING_DOCK", Icon = "🚚", DisplayOrder = 30, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Niveleur de quai", Code = "DOCK_LEVELER", Icon = "⬍", DisplayOrder = 31, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Sas d'étanchéité", Code = "DOCK_SHELTER", Icon = "🚪", DisplayOrder = 32, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Toiture", Code = "ROOF", Icon = "🏠", DisplayOrder = 40, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Lanterneau", Code = "SKYLIGHT", Icon = "🪟", DisplayOrder = 41, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Exutoire de fumée", Code = "SMOKE_VENT", Icon = "💨", DisplayOrder = 42, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Réseau électrique", Code = "ELECTRICAL_NETWORK", Icon = "⚡", DisplayOrder = 50, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Réseau eau", Code = "WATER_NETWORK", Icon = "💧", DisplayOrder = 51, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Réseau gaz", Code = "GAS_NETWORK", Icon = "🔥", DisplayOrder = 52, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Réseau air comprimé", Code = "COMPRESSED_AIR_NETWORK", Icon = "🌀", DisplayOrder = 53, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Réseau assainissement", Code = "SEWAGE_NETWORK", Icon = "♻️", DisplayOrder = 54, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Parking", Code = "PARKING", Icon = "🅿️", DisplayOrder = 60, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Borne de recharge", Code = "CHARGING_STATION", Icon = "🔌", DisplayOrder = 61, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Éclairage extérieur", Code = "OUTDOOR_LIGHTING", Icon = "💡", DisplayOrder = 62, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customer.Id, EquipmentCategoryId = infraCat.Id, Name = "Voirie", Code = "ROAD", Icon = "🛣️", DisplayOrder = 63, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow }
                });

                _context.EquipmentTypes.AddRange(types);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"[INSCRIPTION] {types.Count} types d'équipements créés pour {customer.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[INSCRIPTION] Erreur lors de l'initialisation des catégories d'équipements pour {customer.Name}");
                // Ne pas bloquer l'inscription si l'initialisation des équipements échoue
            }

            // Envoyer l'email de bienvenue
            try
            {
                var emailSent = await _emailService.SendWelcomeEmailAsync(request.Email, request.FirstName);
                if (emailSent)
                {
                    _logger.LogInformation($"[EMAIL] Email de bienvenue envoyé à {request.Email}");
                }
                else
                {
                    _logger.LogWarning($"[EMAIL] Échec de l'envoi de l'email de bienvenue à {request.Email}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[EMAIL] Erreur lors de l'envoi de l'email de bienvenue à {request.Email}");
            }

            // Envoyer la notification à l'admin
            try
            {
                var adminNotificationSent = await _emailService.SendAdminNewAccountNotificationAsync(
                    customer.Name,
                    $"{request.FirstName} {request.LastName}",
                    request.Email,
                    request.Siret
                );
                if (adminNotificationSent)
                {
                    _logger.LogInformation($"[ADMIN EMAIL] Notification nouveau compte envoyée pour {request.Email}");
                }
                else
                {
                    _logger.LogWarning($"[ADMIN EMAIL] Échec de l'envoi de la notification nouveau compte pour {request.Email}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[ADMIN EMAIL] Erreur lors de l'envoi de la notification nouveau compte pour {request.Email}");
            }

            return Ok(new RegisterProfessionalResponse
            {
                Success = true,
                Message = "Inscription réussie ! Votre compte a été créé.",
                CustomerId = customer.Id,
                UserId = user.Id,
                CompanyName = customer.Name
            });
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register(CreateUserRequest request)
        {
            try
            {
                // Vérifier si le username existe déjà pour ce customer
                if (!string.IsNullOrWhiteSpace(request.Username))
                {
                    var existingUsername = await _context.Users
                        .FirstOrDefaultAsync(u => u.Username == request.Username && u.CustomerId == request.CustomerId);

                    if (existingUsername != null)
                    {
                        return BadRequest(new { message = "Ce pseudo est déjà utilisé" });
                    }
                }

                // Vérifier si l'email existe déjà (s'il est fourni)
                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    var existingEmail = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == request.Email);

                    if (existingEmail != null)
                    {
                        return BadRequest(new { message = "Cet email est déjà utilisé" });
                    }
                }

                // Valider que le SectorId existe
                var sectorExists = await _context.Sectors
                    .AnyAsync(s => s.Id == request.SectorId && s.IsActive);

                if (!sectorExists)
                {
                    return BadRequest(new { message = "Le secteur sélectionné n'existe pas ou est inactif" });
                }

                // Valider que l'IndustryId existe
                var industryExists = await _context.Industries
                    .AnyAsync(i => i.Id == request.IndustryId && i.IsActive);

                if (!industryExists)
                {
                    return BadRequest(new { message = "Le métier sélectionné n'existe pas ou est inactif" });
                }

                // Valider que le SiteId existe (si fourni)
                if (request.SiteId.HasValue)
                {
                    var siteExists = await _context.Sites
                        .AnyAsync(s => s.Id == request.SiteId.Value && s.CustomerId == request.CustomerId && s.IsActive);

                    if (!siteExists)
                    {
                        return BadRequest(new { message = "Le site sélectionné n'existe pas ou est inactif" });
                    }
                }

                // Valider que le CustomerId existe
                var customerExists = await _context.Customers
                    .AnyAsync(c => c.Id == request.CustomerId && c.IsActive);

                if (!customerExists)
                {
                    return BadRequest(new { message = "Le client n'existe pas ou est inactif" });
                }

                // Normaliser le numéro de téléphone
                var normalizedPhone = NormalizePhoneNumber(request.Phone);

                // Créer l'utilisateur
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = request.Email ?? "",
                    Username = request.Username,
                    Nom = request.Nom,
                    Prenom = request.Prenom,
                    Tel = normalizedPhone,
                    Role = request.Role,
                    Niveau = request.Role == "ADMIN" || request.Role == "MANAGER" ? "Admin" : "User",
                    SectorId = request.SectorId,
                    IndustryId = request.IndustryId,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    CustomerId = request.CustomerId,
                    SiteId = request.SiteId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    RequiresPasswordChange = true // Première connexion obligatoire de changer le mot de passe
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return Ok(new {
                    message = "Utilisateur créé avec succès",
                    userId = user.Id,
                    username = user.Username
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création d'utilisateur via AuthController.Register");
                return StatusCode(500, new { message = "Erreur lors de la création de l'utilisateur", error = ex.Message });
            }
        }

        [HttpPost("change-password")]
        public async Task<ActionResult> ChangePassword(ChangePasswordRequest request)
        {
            // Récupérer l'utilisateur
            var user = await _context.Users.FindAsync(request.UserId);

            if (user == null)
            {
                return NotFound(new { message = "Utilisateur introuvable" });
            }

            // Vérifier l'ancien mot de passe
            bool oldPasswordValid = BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash);

            if (!oldPasswordValid)
            {
                return BadRequest(new { message = "Ancien mot de passe incorrect" });
            }

            // Mettre à jour le mot de passe
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.RequiresPasswordChange = false;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Mot de passe changé avec succès" });
        }

        [HttpPost("first-login")]
        public async Task<ActionResult<LoginResponse>> FirstLogin(FirstLoginRequest request)
        {
            // Récupérer l'utilisateur par username (insensible à la casse)
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username != null && u.Username.ToLower() == request.Username.ToLower());

            if (user == null)
            {
                return BadRequest(new { message = "Pseudo incorrect" });
            }

            // Vérifier que le PIN existe
            if (string.IsNullOrEmpty(user.SetupPin))
            {
                return BadRequest(new { message = "Aucun code PIN configuré pour cet utilisateur" });
            }

            // Vérifier que le PIN n'a pas expiré
            if (user.SetupPinExpiresAt == null || user.SetupPinExpiresAt < DateTime.UtcNow)
            {
                return BadRequest(new { message = "Le code PIN a expiré. Veuillez contacter votre administrateur." });
            }

            // Vérifier que le PIN correspond
            if (user.SetupPin != request.SetupPin)
            {
                return BadRequest(new { message = "Code PIN incorrect" });
            }

            // Valider le nouveau mot de passe (minimum 8 caractères)
            if (request.NewPassword.Length < 8)
            {
                return BadRequest(new { message = "Le mot de passe doit contenir au moins 8 caractères" });
            }

            // Mettre à jour le mot de passe et supprimer le PIN
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.SetupPin = null;
            user.SetupPinExpiresAt = null;
            user.RequiresPasswordChange = false;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"[FIRST LOGIN] Première connexion réussie pour {user.Username} ({user.Email})");

            // Générer le token JWT pour connecter automatiquement l'utilisateur
            var token = GenerateJwtToken(user);

            return Ok(new LoginResponse
            {
                Token = token,
                RequiresPasswordChange = false,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Username = user.Username,
                    Nom = user.Nom,
                    Prenom = user.Prenom,
                    Niveau = user.Niveau,
                    Role = user.Role,
                    CustomerId = user.CustomerId
                }
            });
        }
    }
}
