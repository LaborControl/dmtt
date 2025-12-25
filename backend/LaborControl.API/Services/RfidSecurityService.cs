using System.Security.Cryptography;
using System.Text;

namespace LaborControl.API.Services
{
    /// <summary>
    /// Service de sécurité RFID pour la génération et validation des checksums HMAC-SHA256
    /// Implémente la stratégie anti-clonage pour les puces NTAG 213
    /// </summary>
    public interface IRfidSecurityService
    {
        /// <summary>
        /// Génère un Salt unique (UUID)
        /// </summary>
        string GenerateSalt();

        /// <summary>
        /// Génère un checksum HMAC-SHA256 basé sur UID + Salt + ChipId + clé secrète
        /// </summary>
        string GenerateChecksum(string uid, string salt, string chipId);

        /// <summary>
        /// Valide un checksum en le comparant avec le checksum attendu
        /// </summary>
        bool ValidateChecksum(string uid, string salt, string chipId, string checksumToValidate);

        /// <summary>
        /// Génère un ChipId unique au format LC-YYYY-MM-NNNNN
        /// </summary>
        string GenerateChipId();

        /// <summary>
        /// Génère une clé unique pour une puce spécifique (6 octets pour Mifare Classic)
        /// Format: Premiers 6 octets de SHA256(ChipId + MasterKey)
        /// </summary>
        byte[] GenerateChipKey(string chipId);
    }

    public class RfidSecurityService : IRfidSecurityService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RfidSecurityService> _logger;
        private readonly string _secretKey;
        private readonly string _masterKey;

        public RfidSecurityService(IConfiguration configuration, ILogger<RfidSecurityService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Récupérer la clé secrète depuis la configuration
            _secretKey = _configuration["Rfid:SecretKey"]
                ?? throw new InvalidOperationException("Rfid:SecretKey non configurée");

            _masterKey = _configuration["Rfid:MasterKey"]
                ?? throw new InvalidOperationException("Rfid:MasterKey non configurée");

            if (_secretKey.Length < 32)
            {
                _logger.LogWarning("⚠️ Clé RFID SecretKey trop courte (< 32 caractères). Recommandé: 256 bits minimum");
            }

            if (_masterKey.Length < 32)
            {
                _logger.LogWarning("⚠️ Clé RFID MasterKey trop courte (< 32 caractères). Recommandé: 256 bits minimum");
            }
        }

        /// <summary>
        /// Génère un Salt unique (UUID v4)
        /// </summary>
        public string GenerateSalt()
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Génère un checksum HMAC-SHA256
        /// Format: HMAC-SHA256(UID + Salt + ChipId, SecretKey)
        /// SANS DATE pour que le checksum reste valide indéfiniment
        /// </summary>
        public string GenerateChecksum(string uid, string salt, string chipId)
        {
            try
            {
                // Construire le message à signer (sans date pour validité permanente)
                var message = $"{uid}{salt}{chipId}";

                // Générer HMAC-SHA256
                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey)))
                {
                    var messageBytes = Encoding.UTF8.GetBytes(message);
                    var hashBytes = hmac.ComputeHash(messageBytes);

                    // Retourner en Base64 (plus compact que hex)
                    var checksum = Convert.ToBase64String(hashBytes);

                    _logger.LogDebug($"✅ Checksum généré pour UID: {uid}, ChipId: {chipId}");

                    return checksum;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur génération checksum pour UID: {uid}");
                throw;
            }
        }

        /// <summary>
        /// Valide un checksum en le comparant avec le checksum attendu
        /// Utilise une comparaison timing-safe pour éviter les attaques par timing
        /// </summary>
        public bool ValidateChecksum(string uid, string salt, string chipId, string checksumToValidate)
        {
            try
            {
                var expectedChecksum = GenerateChecksum(uid, salt, chipId);

                // Comparaison timing-safe (évite les attaques par timing)
                var isValid = CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(expectedChecksum),
                    Encoding.UTF8.GetBytes(checksumToValidate)
                );

                if (isValid)
                {
                    _logger.LogDebug($"✅ Checksum valide pour UID: {uid}");
                }
                else
                {
                    _logger.LogWarning($"⚠️ Checksum invalide pour UID: {uid} (tentative de clonage?)");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur validation checksum pour UID: {uid}");
                return false;
            }
        }

        /// <summary>
        /// Génère un ChipId unique au format LC-YYYY-MM-NNNNN
        /// Exemple: LC-2025-10-00042
        /// </summary>
        public string GenerateChipId()
        {
            var now = DateTime.UtcNow;
            var randomPart = Random.Shared.Next(1, 100000).ToString("D5");
            return $"LC-{now:yyyy-MM}-{randomPart}";
        }

        /// <summary>
        /// Génère une clé unique pour une puce spécifique
        /// Format: Premiers 6 octets de SHA256(ChipId + MasterKey)
        /// Cette clé est utilisée pour protéger les secteurs de la puce Mifare Classic
        /// </summary>
        public byte[] GenerateChipKey(string chipId)
        {
            try
            {
                var message = $"{chipId}{_masterKey}";

                using (var sha256 = SHA256.Create())
                {
                    var messageBytes = Encoding.UTF8.GetBytes(message);
                    var hashBytes = sha256.ComputeHash(messageBytes);

                    // Prendre les 6 premiers octets pour la clé Mifare Classic
                    var chipKey = new byte[6];
                    Array.Copy(hashBytes, chipKey, 6);

                    _logger.LogDebug($"✅ ChipKey générée pour ChipId: {chipId}");

                    return chipKey;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur génération ChipKey pour ChipId: {chipId}");
                throw;
            }
        }
    }
}
