namespace LaborControl.API.Models
{
    /// <summary>
    /// Énumération des 9 états du cycle de vie des puces RFID
    /// Workflow sans scan à la préparation commande
    /// </summary>
    public enum RfidChipStatus
    {
        /// <summary>
        /// 1. EN_TRANSIT - Puces en cours de livraison du fournisseur (Excel importé, pas reçues physiquement)
        /// </summary>
        EN_TRANSIT = 1,

        /// <summary>
        /// 2. EN_ATELIER - Puces reçues du fournisseur, scannées pour conformité, mais non encodées
        /// </summary>
        EN_ATELIER = 2,

        /// <summary>
        /// 3. EN_STOCK - Puces encodées (Salt + Checksum générés), disponibles pour commandes
        /// CustomerId = NULL, OrderId = NULL (stock anonyme)
        /// </summary>
        EN_STOCK = 3,

        /// <summary>
        /// 4. INACTIVE - Client a scanné la puce, assignée à CustomerId + OrderId, whitelist validée, non affectée à point de contrôle
        /// Transition: EN_STOCK → INACTIVE lors du 1er scan client
        /// </summary>
        INACTIVE = 4,

        /// <summary>
        /// 5. ACTIVE - Assignée à point de contrôle + scan de validation
        /// </summary>
        ACTIVE = 5,

        /// <summary>
        /// 6. RETOUR_SAV - Retournée par client, génère commande garantie 0€
        /// </summary>
        RETOUR_SAV = 6,

        /// <summary>
        /// 7. RECEPTION_SAV - Réception par SAV, autorise réaffectation stock
        /// </summary>
        RECEPTION_SAV = 7,

        /// <summary>
        /// 8. REMPLACEE - Puce remplacée, en attente réception puis archivage
        /// </summary>
        REMPLACEE = 8,

        /// <summary>
        /// 9. ARCHIVEE - Puce archivée (déduite du compte client)
        /// </summary>
        ARCHIVEE = 9
    }
}
