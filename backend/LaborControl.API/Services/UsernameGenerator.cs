using LaborControl.API.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace LaborControl.API.Services
{
    public class UsernameGenerator : IUsernameGenerator
    {
        private readonly ApplicationDbContext _context;

        public UsernameGenerator(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateUniqueUsernameAsync(string prenom, string nom, Guid customerId)
        {
            // 1. Normaliser (supprimer accents, espaces, tirets)
            var normalizedPrenom = NormalizeForUsername(prenom);
            var normalizedNom = NormalizeForUsername(nom);

            // 2. Prendre 3 premières lettres du nom (ou moins si nom court)
            var nomPart = normalizedNom.Length >= 3
                ? normalizedNom.Substring(0, 3)
                : normalizedNom;

            // 3. Construire le username de base
            var baseUsername = $"{normalizedPrenom}{nomPart}";

            // Si le username de base est vide ou trop court, utiliser un fallback
            if (string.IsNullOrWhiteSpace(baseUsername) || baseUsername.Length < 2)
            {
                baseUsername = $"User{Guid.NewGuid().ToString().Substring(0, 8)}";
            }

            // 4. Vérifier l'unicité dans le Customer (pas globale)
            var username = baseUsername;
            var counter = 2;

            while (await _context.Users.AnyAsync(u =>
                u.Username == username && u.CustomerId == customerId))
            {
                username = $"{baseUsername}{counter}";
                counter++;
            }

            return username;
        }

        /// <summary>
        /// Normalise une chaîne pour l'utiliser dans un username
        /// - Supprime les accents
        /// - Supprime espaces, tirets, apostrophes
        /// - Première lettre en majuscule, reste en minuscule
        /// </summary>
        private string NormalizeForUsername(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Supprimer accents (normalisation Unicode)
            var normalized = input.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            var withoutAccents = stringBuilder.ToString().Normalize(NormalizationForm.FormC);

            // Supprimer espaces, tirets, apostrophes, points
            withoutAccents = withoutAccents
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("'", "")
                .Replace(".", "")
                .Replace("_", "");

            // Garder uniquement les lettres et chiffres
            var cleaned = new string(withoutAccents.Where(c => char.IsLetterOrDigit(c)).ToArray());

            // Première lettre en majuscule, reste en minuscule
            return cleaned.Length > 0
                ? char.ToUpper(cleaned[0]) + cleaned.Substring(1).ToLower()
                : string.Empty;
        }
    }
}
