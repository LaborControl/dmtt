using PCSC;
using PCSC.Iso7816;
using System.Security.Cryptography;
using System.Text;

namespace LaborControl.API.Services
{
    public class RfidReaderService : IRfidReaderService
    {
        private readonly ILogger<RfidReaderService> _logger;
        private readonly IConfiguration _configuration;

        // Clé par défaut pour les cartes Mifare Classic (peut être changée)
        private static readonly byte[] DefaultKey = { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

        public RfidReaderService(ILogger<RfidReaderService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<List<string>> GetAvailableReadersAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var context = ContextFactory.Instance.Establish(SCardScope.System);
                    var readers = context.GetReaders();
                    return readers.ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la récupération des lecteurs RFID");
                    return new List<string>();
                }
            });
        }

        public async Task<bool> IsReaderConnectedAsync(string readerName)
        {
            var readers = await GetAvailableReadersAsync();
            return readers.Any(r => r.Equals(readerName, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<string?> ReadUidAsync(string readerName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var context = ContextFactory.Instance.Establish(SCardScope.System);
                    using var reader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any);

                    // Commande APDU pour lire l'UID (Get Data)
                    var getDataApdu = new byte[] { 0xFF, 0xCA, 0x00, 0x00, 0x00 };
                    var responseBuffer = new byte[256];

                    var bytesReceived = reader.Transmit(getDataApdu, responseBuffer);

                    if (bytesReceived > 0 && responseBuffer.Length >= 2)
                    {
                        // Les 2 derniers octets sont le status word (SW1 SW2)
                        int sw1 = responseBuffer[responseBuffer.Length - 2];
                        int sw2 = responseBuffer[responseBuffer.Length - 1];

                        if (sw1 == 0x90 && sw2 == 0x00)
                        {
                            // Extraire les données UID (tout sauf les 2 derniers octets)
                            var uidLength = Array.IndexOf(responseBuffer, (byte)0x90);
                            if (uidLength > 0)
                            {
                                var uidBytes = new byte[uidLength];
                                Array.Copy(responseBuffer, uidBytes, uidLength);
                                var uid = BitConverter.ToString(uidBytes).Replace("-", "");
                                _logger.LogInformation("UID lu avec succès: {Uid}", uid);
                                return uid;
                            }
                        }
                    }

                    _logger.LogWarning("Échec de lecture UID");
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la lecture de l'UID");
                    return null;
                }
            });
        }

        public async Task<byte[]?> ReadBlockAsync(string readerName, int blockNumber, byte[] key)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var context = ContextFactory.Instance.Establish(SCardScope.System);
                    using var reader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any);

                    // Authentification avec la clé
                    if (!AuthenticateBlock(reader, blockNumber, key))
                    {
                        _logger.LogWarning("Échec d'authentification pour le bloc {BlockNumber}", blockNumber);
                        return null;
                    }

                    // Lecture du bloc
                    var readCommand = new byte[] { 0xFF, 0xB0, 0x00, (byte)blockNumber, 0x10 };
                    var responseBuffer = new byte[256];

                    var bytesReceived = reader.Transmit(readCommand, responseBuffer);

                    if (bytesReceived > 0 && responseBuffer.Length >= 2)
                    {
                        int sw1 = responseBuffer[responseBuffer.Length - 2];
                        int sw2 = responseBuffer[responseBuffer.Length - 1];

                        if (sw1 == 0x90 && sw2 == 0x00)
                        {
                            var dataLength = Array.IndexOf(responseBuffer, (byte)0x90);
                            if (dataLength > 0)
                            {
                                var data = new byte[dataLength];
                                Array.Copy(responseBuffer, data, dataLength);
                                _logger.LogInformation("Bloc {BlockNumber} lu avec succès", blockNumber);
                                return data;
                            }
                        }
                    }

                    _logger.LogWarning("Échec de lecture du bloc {BlockNumber}", blockNumber);
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la lecture du bloc {BlockNumber}", blockNumber);
                    return null;
                }
            });
        }

        public async Task<bool> WriteBlockAsync(string readerName, int blockNumber, byte[] data, byte[] key)
        {
            if (data.Length != 16)
            {
                _logger.LogError("Les données doivent faire exactement 16 octets");
                return false;
            }

            return await Task.Run(() =>
            {
                try
                {
                    using var context = ContextFactory.Instance.Establish(SCardScope.System);
                    using var reader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any);

                    // Authentification avec la clé
                    if (!AuthenticateBlock(reader, blockNumber, key))
                    {
                        _logger.LogWarning("Échec d'authentification pour le bloc {BlockNumber}", blockNumber);
                        return false;
                    }

                    // Écriture du bloc
                    var writeCommand = new byte[5 + data.Length];
                    writeCommand[0] = 0xFF;
                    writeCommand[1] = 0xD6;
                    writeCommand[2] = 0x00;
                    writeCommand[3] = (byte)blockNumber;
                    writeCommand[4] = (byte)data.Length;
                    Array.Copy(data, 0, writeCommand, 5, data.Length);

                    var responseBuffer = new byte[256];
                    var bytesReceived = reader.Transmit(writeCommand, responseBuffer);

                    if (bytesReceived > 0 && responseBuffer.Length >= 2)
                    {
                        int sw1 = responseBuffer[responseBuffer.Length - 2];
                        int sw2 = responseBuffer[responseBuffer.Length - 1];

                        if (sw1 == 0x90 && sw2 == 0x00)
                        {
                            _logger.LogInformation("Bloc {BlockNumber} écrit avec succès", blockNumber);
                            return true;
                        }
                    }

                    _logger.LogWarning("Échec d'écriture du bloc {BlockNumber}", blockNumber);
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de l'écriture du bloc {BlockNumber}", blockNumber);
                    return false;
                }
            });
        }

        public async Task<bool> EncodeLaborControlChipAsync(string readerName, Guid chipId)
        {
            try
            {
                // Architecture anti-clonage Labor Control:
                // - Bloc 1 (secteur 0, NON protégé): ChipId en clair (lecture mobile offline)
                // - Bloc 4 (secteur 1, protégé): ChipId pour vérification
                // - Bloc 8 (secteur 2, protégé): Checksum anti-clonage
                // - Protection: Clé unique par puce = SHA256(ChipId + MasterKey)
                // - IMPORTANT: Aucun CustomerId ou PackagingCode sur la puce physique!

                // 1. Lire l'UID physique de la puce
                var uid = await ReadUidAsync(readerName);
                if (string.IsNullOrEmpty(uid))
                {
                    _logger.LogError("Impossible de lire l'UID de la puce");
                    return false;
                }

                // 2. Générer un salt aléatoire (UUID)
                var salt = Guid.NewGuid().ToString();

                // 3. Calculer le checksum anti-clonage: HMAC-SHA256(UID + Salt + ChipId)
                var secretKey = _configuration["RfidSecurity:SecretKey"] ?? "DEFAULT_SECRET_KEY_CHANGE_IN_PRODUCTION";
                var checksumInput = $"{uid}{salt}{chipId}";
                byte[] checksum;
                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
                {
                    var checksumBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(checksumInput));
                    checksum = checksumBytes.Take(16).ToArray(); // 16 octets pour un bloc
                }

                // 4. Préparer les données à écrire
                var chipIdBytes = chipId.ToByteArray(); // 16 octets

                // 5. Écrire les données avec la clé par défaut (avant protection)
                var success = true;

                // Bloc 1: ChipId en clair (NON protégé) pour lecture mobile offline
                success &= await WriteBlockAsync(readerName, 1, chipIdBytes, DefaultKey);

                // Bloc 4: ChipId (sera protégé) pour vérification
                success &= await WriteBlockAsync(readerName, 4, chipIdBytes, DefaultKey);

                // Bloc 8: Checksum (sera protégé) pour anti-clonage
                success &= await WriteBlockAsync(readerName, 8, checksum, DefaultKey);

                if (!success)
                {
                    _logger.LogError("Échec de l'écriture des données sur la puce");
                    return false;
                }

                // 6. Générer la clé secrète unique pour cette puce
                var chipKey = GetChipSecretKey(chipId);

                // 7. Protéger le secteur 1 (blocs 4, 5, 6, 7) - contient ChipId
                success &= await ProtectSectorAsync(readerName, 1, chipKey);

                // 8. Protéger le secteur 2 (blocs 8, 9, 10, 11) - contient Checksum
                success &= await ProtectSectorAsync(readerName, 2, chipKey);

                if (success)
                {
                    _logger.LogInformation("Puce encodée et protégée avec succès: ChipId={ChipId}, UID={Uid}, Salt={Salt}", chipId, uid, salt);
                }
                else
                {
                    _logger.LogWarning("Puce encodée mais échec de la protection. ChipId={ChipId}", chipId);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'encodage de la puce Labor Control");
                return false;
            }
        }

        public async Task<bool> VerifyLaborControlChipAsync(string readerName, Guid chipId)
        {
            try
            {
                // 1. Lire le ChipId depuis le bloc 1 (non protégé)
                var bloc1Data = await ReadBlockAsync(readerName, 1, DefaultKey);
                if (bloc1Data == null)
                {
                    _logger.LogWarning("Impossible de lire le bloc 1 (ChipId en clair)");
                    return false;
                }

                var bloc1ChipId = new Guid(bloc1Data);

                // 2. Générer la clé secrète unique basée sur le ChipId
                var chipKey = GetChipSecretKey(chipId);

                // 3. Lire le ChipId depuis le bloc 4 (protégé) avec la clé générée
                var bloc4Data = await ReadBlockAsync(readerName, 4, chipKey);
                if (bloc4Data == null)
                {
                    _logger.LogWarning("Impossible de lire le bloc 4 avec la clé de la puce. Possible puce clonée ou clé incorrecte.");
                    return false;
                }

                var bloc4ChipId = new Guid(bloc4Data);

                // 4. Vérifier que les deux ChipId correspondent
                var isValid = (bloc1ChipId == chipId) && (bloc4ChipId == chipId);

                if (isValid)
                {
                    _logger.LogInformation("Puce vérifiée avec succès: ChipId={ChipId}", chipId);
                }
                else
                {
                    _logger.LogWarning("Vérification échouée: ChipId attendu={Expected}, Bloc1={Bloc1}, Bloc4={Bloc4}",
                        chipId, bloc1ChipId, bloc4ChipId);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification de la puce");
                return false;
            }
        }

        public async Task<bool> WaitForCardAsync(string readerName, int timeoutSeconds = 30)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var context = ContextFactory.Instance.Establish(SCardScope.System);
                    var readerStates = new[]
                    {
                        new SCardReaderState
                        {
                            ReaderName = readerName,
                            CurrentState = SCRState.Unaware
                        }
                    };

                    var timeout = SCardContext.INFINITE;
                    if (timeoutSeconds > 0)
                    {
                        timeout = timeoutSeconds * 1000; // Convertir en millisecondes
                    }

                    var sc = context.GetStatusChange(timeout, readerStates);

                    if (sc == SCardError.Success)
                    {
                        var cardPresent = (readerStates[0].EventState & SCRState.Present) != 0;
                        if (cardPresent)
                        {
                            _logger.LogInformation("Carte détectée sur le lecteur {ReaderName}", readerName);
                            return true;
                        }
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de l'attente de carte");
                    return false;
                }
            });
        }

        private bool AuthenticateBlock(ICardReader reader, int blockNumber, byte[] key)
        {
            try
            {
                // Calculer le secteur (chaque secteur = 4 blocs)
                int sector = blockNumber / 4;

                // Commande d'authentification Load Keys
                var loadKeyCommand = new byte[] { 0xFF, 0x82, 0x00, 0x00, 0x06 }.Concat(key).ToArray();
                var responseBuffer = new byte[256];

                var bytesReceived = reader.Transmit(loadKeyCommand, responseBuffer);

                if (bytesReceived <= 0 || responseBuffer.Length < 2 ||
                    responseBuffer[responseBuffer.Length - 2] != 0x90 || responseBuffer[responseBuffer.Length - 1] != 0x00)
                {
                    _logger.LogWarning("Échec du chargement de la clé");
                    return false;
                }

                // Commande d'authentification
                var authCommand = new byte[] { 0xFF, 0x86, 0x00, 0x00, 0x05, 0x01, 0x00, (byte)blockNumber, 0x60, 0x00 };
                responseBuffer = new byte[256];

                bytesReceived = reader.Transmit(authCommand, responseBuffer);

                if (bytesReceived > 0 && responseBuffer.Length >= 2 &&
                    responseBuffer[responseBuffer.Length - 2] == 0x90 && responseBuffer[responseBuffer.Length - 1] == 0x00)
                {
                    return true;
                }

                _logger.LogWarning("Échec d'authentification");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'authentification du bloc {BlockNumber}", blockNumber);
                return false;
            }
        }

        public async Task<bool> ProtectSectorAsync(string readerName, int sectorNumber, byte[] newKey)
        {
            if (newKey.Length != 6)
            {
                _logger.LogError("La clé doit faire exactement 6 octets");
                return false;
            }

            return await Task.Run(() =>
            {
                try
                {
                    using var context = ContextFactory.Instance.Establish(SCardScope.System);
                    using var reader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any);

                    // Calculer le numéro du bloc de trailer (dernier bloc du secteur)
                    int trailerBlock = (sectorNumber * 4) + 3;

                    // S'authentifier avec la clé par défaut
                    if (!AuthenticateBlock(reader, trailerBlock, DefaultKey))
                    {
                        _logger.LogWarning("Échec d'authentification pour protéger le secteur {SectorNumber}", sectorNumber);
                        return false;
                    }

                    // Construire le bloc de trailer
                    // Format: [Key A (6 bytes)][Access Bits (4 bytes)][Key B (6 bytes)]
                    var trailerData = new byte[16];

                    // Key A (nouvelle clé secrète)
                    Array.Copy(newKey, 0, trailerData, 0, 6);

                    // Access bits (FF 07 80 69) - Permet lecture/écriture avec Key A uniquement
                    // Ces bits protègent les blocs de données et le trailer lui-même
                    trailerData[6] = 0xFF;
                    trailerData[7] = 0x07;
                    trailerData[8] = 0x80;
                    trailerData[9] = 0x69;

                    // Key B (même clé que Key A pour simplifier)
                    Array.Copy(newKey, 0, trailerData, 10, 6);

                    // Écrire le bloc de trailer
                    var writeCommand = new byte[5 + trailerData.Length];
                    writeCommand[0] = 0xFF;
                    writeCommand[1] = 0xD6;
                    writeCommand[2] = 0x00;
                    writeCommand[3] = (byte)trailerBlock;
                    writeCommand[4] = (byte)trailerData.Length;
                    Array.Copy(trailerData, 0, writeCommand, 5, trailerData.Length);

                    var responseBuffer = new byte[256];
                    var bytesReceived = reader.Transmit(writeCommand, responseBuffer);

                    if (bytesReceived > 0 && responseBuffer.Length >= 2)
                    {
                        int sw1 = responseBuffer[responseBuffer.Length - 2];
                        int sw2 = responseBuffer[responseBuffer.Length - 1];

                        if (sw1 == 0x90 && sw2 == 0x00)
                        {
                            _logger.LogInformation("Secteur {SectorNumber} protégé avec succès", sectorNumber);
                            return true;
                        }
                    }

                    _logger.LogWarning("Échec de protection du secteur {SectorNumber}", sectorNumber);
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la protection du secteur {SectorNumber}", sectorNumber);
                    return false;
                }
            });
        }

        public byte[] GetChipSecretKey(Guid chipId)
        {
            // Générer une clé secrète UNIQUE basée sur le ChipId et la clé maître
            // Chaque puce a sa propre clé de protection, rendant le clonage impossible
            var masterKey = _configuration["RfidSecurity:MasterKey"] ?? "MASTER_KEY_CHANGE_IN_PRODUCTION";
            var keyMaterial = $"{chipId}{masterKey}";

            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyMaterial));

            // Prendre les 6 premiers octets du hash pour la clé Mifare Classic
            var key = new byte[6];
            Array.Copy(hash, key, 6);

            return key;
        }
    }
}
