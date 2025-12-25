using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LaborControl.API.DTOs;
using LaborControl.API.Services;

namespace LaborControl.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RfidReaderController : ControllerBase
    {
        private readonly IRfidReaderService _rfidReaderService;
        private readonly ILogger<RfidReaderController> _logger;

        public RfidReaderController(IRfidReaderService rfidReaderService, ILogger<RfidReaderController> logger)
        {
            _rfidReaderService = rfidReaderService;
            _logger = logger;
        }

        [HttpGet("readers")]
        public async Task<ActionResult<RfidReadersResponse>> GetReaders()
        {
            try
            {
                var readers = await _rfidReaderService.GetAvailableReadersAsync();
                return Ok(new RfidReadersResponse
                {
                    Readers = readers,
                    Success = true,
                    Message = readers.Any() ? $"{readers.Count} lecteur(s) trouvé(s)" : "Aucun lecteur trouvé"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des lecteurs RFID");
                return StatusCode(500, new RfidReadersResponse
                {
                    Success = false,
                    Message = "Erreur lors de la récupération des lecteurs RFID"
                });
            }
        }

        [HttpPost("read-uid")]
        public async Task<ActionResult<RfidReadUidResponse>> ReadUid([FromBody] RfidReadUidRequest request)
        {
            try
            {
                var uid = await _rfidReaderService.ReadUidAsync(request.ReaderName);

                if (uid == null)
                {
                    return Ok(new RfidReadUidResponse
                    {
                        Success = false,
                        Message = "Impossible de lire l'UID. Assurez-vous qu'une carte est présente sur le lecteur."
                    });
                }

                return Ok(new RfidReadUidResponse
                {
                    Uid = uid,
                    Success = true,
                    Message = "UID lu avec succès"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la lecture de l'UID");
                return StatusCode(500, new RfidReadUidResponse
                {
                    Success = false,
                    Message = "Erreur lors de la lecture de l'UID"
                });
            }
        }

        [HttpPost("read-block")]
        public async Task<ActionResult<RfidReadBlockResponse>> ReadBlock([FromBody] RfidReadBlockRequest request)
        {
            try
            {
                var key = request.Key ?? new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
                var data = await _rfidReaderService.ReadBlockAsync(request.ReaderName, request.BlockNumber, key);

                if (data == null)
                {
                    return Ok(new RfidReadBlockResponse
                    {
                        Success = false,
                        Message = $"Impossible de lire le bloc {request.BlockNumber}"
                    });
                }

                return Ok(new RfidReadBlockResponse
                {
                    Data = data,
                    DataHex = BitConverter.ToString(data).Replace("-", ""),
                    Success = true,
                    Message = $"Bloc {request.BlockNumber} lu avec succès"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la lecture du bloc {BlockNumber}", request.BlockNumber);
                return StatusCode(500, new RfidReadBlockResponse
                {
                    Success = false,
                    Message = "Erreur lors de la lecture du bloc"
                });
            }
        }

        [HttpPost("write-block")]
        public async Task<ActionResult<RfidWriteBlockResponse>> WriteBlock([FromBody] RfidWriteBlockRequest request)
        {
            try
            {
                if (request.Data.Length != 16)
                {
                    return BadRequest(new RfidWriteBlockResponse
                    {
                        Success = false,
                        Message = "Les données doivent faire exactement 16 octets"
                    });
                }

                var key = request.Key ?? new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
                var success = await _rfidReaderService.WriteBlockAsync(request.ReaderName, request.BlockNumber, request.Data, key);

                if (!success)
                {
                    return Ok(new RfidWriteBlockResponse
                    {
                        Success = false,
                        Message = $"Impossible d'écrire le bloc {request.BlockNumber}"
                    });
                }

                return Ok(new RfidWriteBlockResponse
                {
                    Success = true,
                    Message = $"Bloc {request.BlockNumber} écrit avec succès"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'écriture du bloc {BlockNumber}", request.BlockNumber);
                return StatusCode(500, new RfidWriteBlockResponse
                {
                    Success = false,
                    Message = "Erreur lors de l'écriture du bloc"
                });
            }
        }

        [HttpPost("encode-chip")]
        public async Task<ActionResult<RfidEncodeChipResponse>> EncodeChip([FromBody] RfidEncodeChipRequest request)
        {
            try
            {
                // D'abord lire l'UID
                var uid = await _rfidReaderService.ReadUidAsync(request.ReaderName);
                if (uid == null)
                {
                    return Ok(new RfidEncodeChipResponse
                    {
                        Success = false,
                        Message = "Impossible de lire l'UID. Assurez-vous qu'une carte est présente sur le lecteur."
                    });
                }

                // Encoder la puce (uniquement ChipId + Checksum anti-clonage)
                // IMPORTANT: Pas de CustomerId ou PackagingCode sur la puce physique!
                var success = await _rfidReaderService.EncodeLaborControlChipAsync(
                    request.ReaderName,
                    request.ChipId
                );

                if (!success)
                {
                    return Ok(new RfidEncodeChipResponse
                    {
                        Success = false,
                        Message = "Erreur lors de l'encodage de la puce"
                    });
                }

                return Ok(new RfidEncodeChipResponse
                {
                    Success = true,
                    Message = "Puce encodée et protégée avec succès (clé unique par puce)",
                    Uid = uid
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'encodage de la puce");
                return StatusCode(500, new RfidEncodeChipResponse
                {
                    Success = false,
                    Message = "Erreur lors de l'encodage de la puce"
                });
            }
        }

        [HttpPost("verify-chip")]
        public async Task<ActionResult<RfidVerifyChipResponse>> VerifyChip([FromBody] RfidVerifyChipRequest request)
        {
            try
            {
                // D'abord lire l'UID
                var uid = await _rfidReaderService.ReadUidAsync(request.ReaderName);
                if (uid == null)
                {
                    return Ok(new RfidVerifyChipResponse
                    {
                        Success = false,
                        IsValid = false,
                        Message = "Impossible de lire l'UID. Assurez-vous qu'une carte est présente sur le lecteur."
                    });
                }

                // Vérifier la puce (vérifie ChipId bloc 1 et bloc 4 avec clé unique)
                var isValid = await _rfidReaderService.VerifyLaborControlChipAsync(request.ReaderName, request.ChipId);

                return Ok(new RfidVerifyChipResponse
                {
                    Success = true,
                    IsValid = isValid,
                    Message = isValid ? "Puce valide - Protection anti-clonage OK" : "Puce invalide, non encodée ou clonée",
                    Uid = uid
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification de la puce");
                return StatusCode(500, new RfidVerifyChipResponse
                {
                    Success = false,
                    IsValid = false,
                    Message = "Erreur lors de la vérification de la puce"
                });
            }
        }

        [HttpPost("wait-for-card")]
        public async Task<ActionResult<RfidWaitForCardResponse>> WaitForCard([FromBody] RfidWaitForCardRequest request)
        {
            try
            {
                var cardPresent = await _rfidReaderService.WaitForCardAsync(request.ReaderName, request.TimeoutSeconds);

                return Ok(new RfidWaitForCardResponse
                {
                    Success = true,
                    CardPresent = cardPresent,
                    Message = cardPresent ? "Carte détectée" : "Aucune carte détectée dans le délai imparti"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'attente de carte");
                return StatusCode(500, new RfidWaitForCardResponse
                {
                    Success = false,
                    CardPresent = false,
                    Message = "Erreur lors de l'attente de carte"
                });
            }
        }
    }
}
