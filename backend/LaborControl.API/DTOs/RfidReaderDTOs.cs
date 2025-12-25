namespace LaborControl.API.DTOs
{
    public class RfidReadersResponse
    {
        public List<string> Readers { get; set; } = new();
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    public class RfidReadUidRequest
    {
        public string ReaderName { get; set; } = string.Empty;
    }

    public class RfidReadUidResponse
    {
        public string? Uid { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    public class RfidReadBlockRequest
    {
        public string ReaderName { get; set; } = string.Empty;
        public int BlockNumber { get; set; }
        public byte[]? Key { get; set; }
    }

    public class RfidReadBlockResponse
    {
        public byte[]? Data { get; set; }
        public string? DataHex { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    public class RfidWriteBlockRequest
    {
        public string ReaderName { get; set; } = string.Empty;
        public int BlockNumber { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public byte[]? Key { get; set; }
    }

    public class RfidWriteBlockResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    public class RfidEncodeChipRequest
    {
        public string ReaderName { get; set; } = string.Empty;
        public Guid ChipId { get; set; }
        // IMPORTANT: Pas de CustomerId ou PackagingCode!
        // Les puces sont encodées AVANT affectation client
        // Seul le ChipId est écrit physiquement sur la puce
    }

    public class RfidEncodeChipResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Uid { get; set; }
    }

    public class RfidVerifyChipRequest
    {
        public string ReaderName { get; set; } = string.Empty;
        public Guid ChipId { get; set; }
        // Pas de CustomerId - la vérification se fait avec la clé unique de la puce
    }

    public class RfidVerifyChipResponse
    {
        public bool Success { get; set; }
        public bool IsValid { get; set; }
        public string? Message { get; set; }
        public string? Uid { get; set; }
    }

    public class RfidWaitForCardRequest
    {
        public string ReaderName { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
    }

    public class RfidWaitForCardResponse
    {
        public bool Success { get; set; }
        public bool CardPresent { get; set; }
        public string? Message { get; set; }
    }
}
