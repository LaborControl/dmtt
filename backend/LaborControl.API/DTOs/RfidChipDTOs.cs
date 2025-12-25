namespace LaborControl.API.DTOs
{
    /// <summary>
    /// Requête pour créer une puce RFID
    /// </summary>
    public class CreateRfidChipRequest
    {
        public string ChipId { get; set; } = string.Empty;
        public string Uid { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public Guid? OrderId { get; set; }
    }

    /// <summary>
    /// Requête pour enregistrer une puce unique (ACR122U)
    /// </summary>
    public class RegisterSingleChipRequest
    {
        public string Uid { get; set; } = string.Empty;
        public Guid? OrderId { get; set; }
    }

    /// <summary>
    /// Requête pour valider un scan NFC
    /// </summary>
    public class ValidateScanRequest
    {
        public string Uid { get; set; } = string.Empty;
    }

    /// <summary>
    /// Réponse pour validation de scan
    /// </summary>
    public class ValidateScanResponse
    {
        public bool IsValid { get; set; }
        public string ChipId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Guid? ControlPointId { get; set; }
    }

    /// <summary>
    /// Réponse pour une puce RFID
    /// </summary>
    public class RfidChipResponse
    {
        public Guid Id { get; set; }
        public string ChipId { get; set; } = string.Empty;
        public string Uid { get; set; } = string.Empty;
        public Guid? CustomerId { get; set; }  // Nullable pour les puces en stock
        public string? CustomerName { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime ActivationDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? PackagingCode { get; set; }
    }

    /// <summary>
    /// Requête pour importer des puces depuis Excel
    /// </summary>
    public class ImportChipsRequest
    {
        public Guid OrderId { get; set; }
        public List<string> Uids { get; set; } = new();
    }

    /// <summary>
    /// Réponse pour import Excel
    /// </summary>
    public class ImportChipsResponse
    {
        public int SuccessCount { get; set; }
        public int DuplicateCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<RfidChipResponse> CreatedChips { get; set; } = new();
    }

    /// <summary>
    /// Requête pour assigner des puces à un client
    /// </summary>
    public class AssignChipsRequest
    {
        public List<Guid> ChipIds { get; set; } = new();
        public Guid CustomerId { get; set; }
        public Guid? OrderId { get; set; }
    }

    /// <summary>
    /// Réponse pour vérifier l'existence d'une puce
    /// </summary>
    public class CheckChipExistsResponse
    {
        public bool Exists { get; set; }
        public RfidChipResponse? Chip { get; set; }
    }

    public class ParseExcelResponse
    {
        public List<string> Uids { get; set; } = new();
        public int TotalRows { get; set; }
    }

    /// <summary>
    /// Requête pour recevoir une puce du fournisseur (État EN_ATELIER)
    /// </summary>
    public class ReceiveFromSupplierRequest
    {
        public Guid SupplierOrderId { get; set; }
    }

    /// <summary>
    /// Requête pour expédier une puce au client (État EN_LIVRAISON)
    /// </summary>
    public class ShipToClientRequest
    {
        public Guid ClientOrderId { get; set; }
    }

    /// <summary>
    /// Requête pour confirmer la livraison d'une puce (État LIVREE)
    /// </summary>
    public class ConfirmDeliveryRequest
    {
        public string PackagingCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Requête pour assigner une puce à un point de contrôle (État ACTIVE)
    /// </summary>
    public class AssignToControlPointRequest
    {
        public Guid ControlPointId { get; set; }
    }

    /// <summary>
    /// Requête pour demander un SAV (État RETOUR_SAV)
    /// </summary>
    public class RequestSavRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Requête pour remplacer une puce (État REMPLACEE)
    /// </summary>
    public class ReplaceChipRequest
    {
        public Guid ReplacementChipId { get; set; }
    }

    /// <summary>
    /// Requête pour archiver une puce avec motif (État ARCHIVEE)
    /// </summary>
    public class ArchiveChipRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Requête pour activer une puce (1er scan client: EN_STOCK → INACTIVE)
    /// </summary>
    public class ActivateChipRequest
    {
        public string Uid { get; set; } = string.Empty;

        /// <summary>
        /// ChipId lu depuis le bloc 1 (non protégé)
        /// </summary>
        public string ChipId { get; set; } = string.Empty;

        /// <summary>
        /// Données lues du bloc 4 (protégé) - ChipId pour vérification anti-clonage
        /// 16 bytes en hexadécimal (32 caractères)
        /// </summary>
        public string Block4Data { get; set; } = string.Empty;

        /// <summary>
        /// Données lues du bloc 8 (protégé) - Checksum HMAC-SHA256
        /// 16 bytes en hexadécimal (32 caractères)
        /// </summary>
        public string Block8Data { get; set; } = string.Empty;
    }

    /// <summary>
    /// Puce dans la whitelist pour validation offline mobile
    /// </summary>
    public class WhitelistedChipDto
    {
        public string ChipId { get; set; } = string.Empty;
        public string? ControlPointId { get; set; }
        public string? ControlPointName { get; set; }
        public DateTime ActivatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Réponse whitelist complète pour un customer
    /// </summary>
    public class WhitelistResponse
    {
        public List<WhitelistedChipDto> Chips { get; set; } = new();
    }

    /// <summary>
    /// Requête pour demander les paramètres d'encodage d'une puce
    /// </summary>
    public class RequestEncodingRequest
    {
        public string Uid { get; set; } = string.Empty;
    }

    /// <summary>
    /// Réponse contenant les paramètres d'encodage générés par le backend
    /// </summary>
    public class EncodingParametersResponse
    {
        public string ChipId { get; set; } = string.Empty;
        public string Salt { get; set; } = string.Empty;
        public string Checksum { get; set; } = string.Empty;
        public byte[] ChipKey { get; set; } = Array.Empty<byte>();
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Réponse contenant les informations d'une puce déjà encodée
    /// </summary>
    public class ChipInfoResponse
    {
        public bool IsEncoded { get; set; }
        public string? ChipId { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid? CustomerId { get; set; }
        public Guid? ControlPointId { get; set; }
        public string? ControlPointName { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
