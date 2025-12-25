namespace LaborControl.API.Services
{
    public interface IRfidReaderService
    {
        /// <summary>
        /// Récupère la liste des lecteurs RFID connectés au système
        /// </summary>
        Task<List<string>> GetAvailableReadersAsync();

        /// <summary>
        /// Vérifie si un lecteur spécifique est connecté
        /// </summary>
        Task<bool> IsReaderConnectedAsync(string readerName);

        /// <summary>
        /// Lit l'UID d'une puce RFID présente sur le lecteur
        /// </summary>
        Task<string?> ReadUidAsync(string readerName);

        /// <summary>
        /// Lit les données d'un bloc spécifique de la puce
        /// </summary>
        Task<byte[]?> ReadBlockAsync(string readerName, int blockNumber, byte[] key);

        /// <summary>
        /// Écrit des données sur un bloc spécifique de la puce
        /// </summary>
        Task<bool> WriteBlockAsync(string readerName, int blockNumber, byte[] data, byte[] key);

        /// <summary>
        /// Encode une puce avec les données Labor Control (ChipId et Checksum uniquement)
        /// Blocs utilisés:
        /// - Bloc 1 (non protégé): ChipId en clair pour lecture mobile offline
        /// - Bloc 4 (protégé): ChipId pour vérification
        /// - Bloc 8 (protégé): Checksum anti-clonage HMAC-SHA256(UID + Salt + ChipId)
        /// Protection: SHA256(ChipId + MasterKey) - clé unique par puce
        /// </summary>
        Task<bool> EncodeLaborControlChipAsync(string readerName, Guid chipId);

        /// <summary>
        /// Vérifie l'intégrité d'une puce Labor Control
        /// Lit le ChipId depuis bloc 1 et vérifie la cohérence avec les blocs protégés
        /// </summary>
        Task<bool> VerifyLaborControlChipAsync(string readerName, Guid chipId);

        /// <summary>
        /// Attend qu'une puce soit détectée sur le lecteur (polling)
        /// </summary>
        Task<bool> WaitForCardAsync(string readerName, int timeoutSeconds = 30);

        /// <summary>
        /// Protège un secteur en changeant la clé d'accès par défaut vers une clé secrète
        /// </summary>
        Task<bool> ProtectSectorAsync(string readerName, int sectorNumber, byte[] newKey);

        /// <summary>
        /// Récupère la clé secrète unique pour une puce basée sur son ChipId
        /// Génère: SHA256(ChipId + MasterKey) -> 6 premiers octets
        /// </summary>
        byte[] GetChipSecretKey(Guid chipId);
    }
}
