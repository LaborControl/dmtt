namespace LaborControl.API.Services
{
    public interface IUsernameGenerator
    {
        /// <summary>
        /// Génère un username unique basé sur le prénom et le nom
        /// Format: PrénomNNN (Prénom + 3 premières lettres du nom)
        /// Exemple: Jean Dupont → JeanDup (ou JeanDup2 si déjà pris)
        /// </summary>
        /// <param name="prenom">Prénom de l'utilisateur</param>
        /// <param name="nom">Nom de l'utilisateur</param>
        /// <param name="customerId">ID du client (pour garantir l'unicité par client)</param>
        /// <returns>Username unique et normalisé</returns>
        Task<string> GenerateUniqueUsernameAsync(string prenom, string nom, Guid customerId);
    }
}
