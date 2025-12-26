using Npgsql;

// Connection string Azure
var connectionString = "Server=laborcontrol-db.postgres.database.azure.com;Database=laborcontrol;Port=5432;User Id=laboradmin;Password=Loulou@2025!;Ssl Mode=Require;";

// Infos Super Admin
var email = "admin@laborcontrol.com";
var password = "Admin@2025";
var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

Console.WriteLine($"Création Super Admin...");
Console.WriteLine($"Email: {email}");
Console.WriteLine($"Hash: {passwordHash}");

using var connection = new NpgsqlConnection(connectionString);
connection.Open();

// Récupérer le premier Customer (ou créer un customer "LABOR CONTROL")
var getCustomerCmd = new NpgsqlCommand(@"
    SELECT ""Id"" FROM ""Customers"" LIMIT 1
", connection);

var customerId = getCustomerCmd.ExecuteScalar()?.ToString();

if (string.IsNullOrEmpty(customerId))
{
    Console.WriteLine("Création du customer LABOR CONTROL...");
    var createCustomerCmd = new NpgsqlCommand(@"
        INSERT INTO ""Customers"" (""Id"", ""Name"", ""Email"", ""CreatedAt"")
        VALUES (gen_random_uuid(), 'LABOR CONTROL', 'contact@labor-control.fr', NOW())
        RETURNING ""Id""
    ", connection);
    customerId = createCustomerCmd.ExecuteScalar()?.ToString();
    Console.WriteLine($"Customer créé: {customerId}");
}

// Créer le Super Admin
var sql = @"
INSERT INTO ""Users"" (
    ""Id"", ""Email"", ""PasswordHash"", ""Nom"", ""Prenom"", 
    ""Tel"", ""Service"", ""Fonction"", ""Niveau"", ""Role"", 
    ""CustomerId"", ""CreatedAt"", ""IsActive""
)
VALUES (
    gen_random_uuid(),
    @email,
    @passwordHash,
    'Administrateur',
    'Super',
    '0600000000',
    'Direction',
    'Super Administrateur',
    'SuperAdmin',
    'User',
    @customerId::uuid,
    NOW(),
    true
)
ON CONFLICT (""Email"") DO UPDATE SET
    ""PasswordHash"" = @passwordHash;
";

using var cmd = new NpgsqlCommand(sql, connection);
cmd.Parameters.AddWithValue("email", email);
cmd.Parameters.AddWithValue("passwordHash", passwordHash);
cmd.Parameters.AddWithValue("customerId", customerId!);

try
{
    cmd.ExecuteNonQuery();
    Console.WriteLine("✅ Super Admin créé avec succès !");
    Console.WriteLine($"Email: {email}");
    Console.WriteLine($"Mot de passe: {password}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ ERREUR: {ex.Message}");
}
