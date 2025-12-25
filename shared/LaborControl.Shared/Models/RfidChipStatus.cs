namespace LaborControl.Shared.Models;

/// <summary>
/// Énumération des états du cycle de vie des puces RFID
/// </summary>
public enum RfidChipStatus
{
    /// <summary>En transit depuis le fournisseur (commandée, UID reçu par Excel)</summary>
    InTransitSupplier = 0,

    /// <summary>En atelier (reçue du fournisseur mais pas encore encodée)</summary>
    InWorkshop = 1,

    /// <summary>En stock (encodée, disponible pour commande client)</summary>
    InStock = 2,

    /// <summary>En livraison client (attribuée à une commande, N° PKG assigné)</summary>
    InDelivery = 3,

    /// <summary>Livrée en attente de validation (réceptionnée par client, en attente validation PKG)</summary>
    DeliveredPending = 4,

    /// <summary>Réceptionnée par client (validation PKG confirmée, ajoutée à liste blanche)</summary>
    Delivered = 5,

    /// <summary>Affectée à un point de contrôle (assignée par manager/superviseur)</summary>
    Assigned = 6,

    /// <summary>Active (puce en service, au moins un scan effectué)</summary>
    Active = 7,

    /// <summary>Désactivée (hors service, désactivation volontaire)</summary>
    Inactive = 8,

    /// <summary>Retour SAV (déclarée en retour par client)</summary>
    SavReturn = 9,

    /// <summary>En réparation SAV (en cours de traitement)</summary>
    SavRepair = 10,

    /// <summary>Remplacée (garantie à vie, puce de remplacement envoyée)</summary>
    SavReplacement = 11
}
