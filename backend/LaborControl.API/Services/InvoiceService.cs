using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using LaborControl.API.Models;

namespace LaborControl.API.Services;

public interface IInvoiceService
{
    Task<string> GenerateInvoicePdfAsync(Order order);
}

public class InvoiceService : IInvoiceService
{
    private readonly ILogger<InvoiceService> _logger;
    private readonly string _invoicesDirectory;
    private readonly string? _logoPath;

    public InvoiceService(ILogger<InvoiceService> logger, IConfiguration configuration)
    {
        _logger = logger;

        // Configurer QuestPDF pour utiliser la licence communautaire
        QuestPDF.Settings.License = LicenseType.Community;

        // Activer le debugging pour identifier les problèmes de layout
        QuestPDF.Settings.EnableDebugging = true;

        // Créer le dossier pour stocker les factures
        var webRootPath = configuration["WebRootPath"] ?? "wwwroot";
        _invoicesDirectory = Path.Combine(webRootPath, "invoices");

        if (!Directory.Exists(_invoicesDirectory))
        {
            Directory.CreateDirectory(_invoicesDirectory);
        }

        // Définir le chemin du logo s'il existe
        var logoPath = Path.Combine(webRootPath, "logo-lc.png");
        _logoPath = File.Exists(logoPath) ? logoPath : null;
    }

    public System.Threading.Tasks.Task<string> GenerateInvoicePdfAsync(Order order)
    {
        try
        {
            if (order.Customer == null)
            {
                throw new InvalidOperationException("Les informations du client sont requises pour générer la facture");
            }

            // Nom du fichier PDF
            var fileName = $"Facture_{order.OrderNumber}_{DateTime.UtcNow:yyyyMMdd}.pdf";
            var filePath = Path.Combine(_invoicesDirectory, fileName);

            // Générer le PDF
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header()
                        .Height(120)
                        .Padding(15)
                        .Row(row =>
                        {
                            // Logo à gauche (si disponible)
                            if (_logoPath != null)
                            {
                                row.ConstantItem(60).AlignMiddle().Image(_logoPath);
                            }

                            // Texte et informations au centre
                            row.RelativeItem().PaddingLeft(_logoPath != null ? 15 : 0).Column(column =>
                            {
                                // Titre "LABOR CONTROL" en 2 couleurs
                                column.Item().Text(text =>
                                {
                                    text.Span("LABOR").FontSize(24).Bold().FontColor("#22D3EE"); // cyan-400
                                    text.Span(" CONTROL").FontSize(24).Bold().FontColor("#1E3A8A"); // blue-800
                                });

                                column.Item().Text("Système de gestion NFC")
                                    .FontSize(10)
                                    .FontColor(Colors.Grey.Darken2);

                                column.Item().PaddingTop(8).Text(text =>
                                {
                                    text.Span("Facture N° ").FontSize(11).Bold();
                                    text.Span(order.OrderNumber).FontSize(11).Bold().FontColor(Colors.Blue.Darken1);
                                });

                                column.Item().Text($"Date: {order.CreatedAt:dd/MM/yyyy}")
                                    .FontSize(9)
                                    .FontColor(Colors.Grey.Darken1);
                            });
                        });

                    page.Content()
                        .PaddingVertical(20)
                        .Column(column =>
                        {
                            // Informations client
                            column.Item().PaddingBottom(20).Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("FACTURÉ À").Bold().FontSize(11).FontColor(Colors.Blue.Darken1);
                                    col.Item().PaddingTop(5).Text(text =>
                                    {
                                        var clientName = order.Customer.Name ?? order.Customer.ContactName ?? "Client";
                                        // Limiter la longueur pour éviter les débordements
                                        text.Span(clientName.Length > 40 ? clientName.Substring(0, 40) + "..." : clientName)
                                            .FontSize(10)
                                            .Bold();
                                    });

                                    if (!string.IsNullOrEmpty(order.Customer.ContactName) && order.Customer.Name != order.Customer.ContactName)
                                    {
                                        var contactName = order.Customer.ContactName.Length > 35 ? order.Customer.ContactName.Substring(0, 35) + "..." : order.Customer.ContactName;
                                        col.Item().Text($"Att: {contactName}").FontSize(9);
                                    }

                                    if (!string.IsNullOrEmpty(order.Customer.ContactEmail))
                                    {
                                        var email = order.Customer.ContactEmail.Length > 40 ? order.Customer.ContactEmail.Substring(0, 40) + "..." : order.Customer.ContactEmail;
                                        col.Item().Text(email).FontSize(9);
                                    }

                                    if (!string.IsNullOrEmpty(order.Customer.ContactPhone))
                                    {
                                        col.Item().Text(order.Customer.ContactPhone).FontSize(9);
                                    }
                                });

                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("ADRESSE DE LIVRAISON").Bold().FontSize(11).FontColor(Colors.Blue.Darken1);
                                    var address = order.DeliveryAddress.Length > 50 ? order.DeliveryAddress.Substring(0, 50) + "..." : order.DeliveryAddress;
                                    col.Item().PaddingTop(5).Text(address).FontSize(9);

                                    if (!string.IsNullOrEmpty(order.DeliveryPostalCode) && !string.IsNullOrEmpty(order.DeliveryCity))
                                    {
                                        col.Item().Text($"{order.DeliveryPostalCode} {order.DeliveryCity}").FontSize(9);
                                    }

                                    if (!string.IsNullOrEmpty(order.DeliveryCountry))
                                    {
                                        col.Item().Text(order.DeliveryCountry).FontSize(9);
                                    }

                                    if (!string.IsNullOrEmpty(order.Service))
                                    {
                                        var service = order.Service.Length > 40 ? order.Service.Substring(0, 40) + "..." : order.Service;
                                        col.Item().PaddingTop(5).Text($"Service: {service}").FontSize(9).Italic();
                                    }
                                });
                            });

                            // Tableau des articles
                            column.Item().PaddingTop(10).Table(table =>
                            {
                                // Définir les colonnes
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3); // Description
                                    columns.RelativeColumn(1); // Quantité
                                    columns.RelativeColumn(1); // Prix unitaire HT
                                    columns.RelativeColumn(1); // TVA
                                    columns.RelativeColumn(1); // Total TTC
                                });

                                // En-tête du tableau
                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Description").FontColor(Colors.White).Bold();
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Qté").FontColor(Colors.White).Bold();
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Prix HT").FontColor(Colors.White).Bold();
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("TVA").FontColor(Colors.White).Bold();
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Total TTC").FontColor(Colors.White).Bold();
                                });

                                // Ligne de produit
                                const decimal TVA_RATE = 0.20m;
                                var totalTTC = order.TotalAmount;
                                var totalHT = totalTTC / (1 + TVA_RATE);
                                var totalTVA = totalTTC - totalHT;

                                // Ligne principale - Puces NFC
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                                    .Text($"Puces NFC NTAG213 - Labor Control\nCommande {order.OrderNumber}");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                                    .AlignCenter().Text(order.ChipsQuantity.ToString());
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                                    .AlignRight().Text($"{totalHT:F2} €");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                                    .AlignRight().Text("20%");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                                    .AlignRight().Text($"{totalTTC:F2} €").Bold();
                            });

                            // Totaux
                            column.Item().PaddingTop(20).AlignRight().Column(col =>
                            {
                                const decimal TVA_RATE = 0.20m;
                                var totalTTC = order.TotalAmount;
                                var totalHT = totalTTC / (1 + TVA_RATE);
                                var totalTVA = totalTTC - totalHT;

                                col.Item().Row(row =>
                                {
                                    row.RelativeItem(3).Text("");
                                    row.RelativeItem(1).Text("Total HT :").Bold();
                                    row.RelativeItem(1).Text($"{totalHT:F2} €").AlignRight();
                                });

                                col.Item().Row(row =>
                                {
                                    row.RelativeItem(3).Text("");
                                    row.RelativeItem(1).Text("TVA (20%) :").Bold();
                                    row.RelativeItem(1).Text($"{totalTVA:F2} €").AlignRight();
                                });

                                col.Item().PaddingTop(5).Row(row =>
                                {
                                    row.RelativeItem(3).Text("");
                                    row.RelativeItem(1).Background(Colors.Blue.Lighten4).Padding(5)
                                        .Text("TOTAL TTC :").Bold().FontSize(12);
                                    row.RelativeItem(1).Background(Colors.Blue.Lighten4).Padding(5)
                                        .Text($"{totalTTC:F2} €").AlignRight().Bold().FontSize(12)
                                        .FontColor(Colors.Blue.Darken2);
                                });
                            });

                            // Informations de paiement
                            if (!string.IsNullOrEmpty(order.StripePaymentIntentId))
                            {
                                column.Item().PaddingTop(20).Text(text =>
                                {
                                    text.Span("Statut du paiement : ").FontSize(10);
                                    text.Span("PAYÉ").Bold().FontColor(Colors.Green.Darken2).FontSize(10);
                                });

                                column.Item().Text($"ID Transaction : {order.StripePaymentIntentId}")
                                    .FontSize(9)
                                    .FontColor(Colors.Grey.Darken1);
                            }

                            // Notes
                            if (!string.IsNullOrEmpty(order.Notes))
                            {
                                column.Item().PaddingTop(15).Column(col =>
                                {
                                    col.Item().Text("Notes :").Bold().FontSize(10);
                                    col.Item().Text(order.Notes).FontSize(9).Italic();
                                });
                            }
                        });

                    page.Footer()
                        .Height(40)
                        .Background(Colors.Grey.Lighten3)
                        .Padding(10)
                        .AlignCenter()
                        .Row(row =>
                        {
                            row.RelativeItem().Text("Merci pour votre commande !")
                                .FontSize(10)
                                .Bold()
                                .FontColor(Colors.Blue.Darken2);

                            row.RelativeItem().Text("www.laborcontrol.com")
                                .FontSize(9)
                                .FontColor(Colors.Blue.Darken1);

                            row.RelativeItem().Text($"{DateTime.Now:dd/MM/yyyy}")
                                .FontSize(8)
                                .FontColor(Colors.Grey.Darken1);
                        });
                });
            }).GeneratePdf(filePath);

            _logger.LogInformation($"Facture PDF générée avec succès : {fileName}");

            // Retourner le chemin relatif pour le stockage en base de données
            return System.Threading.Tasks.Task.FromResult($"/invoices/{fileName}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur lors de la génération de la facture PDF : {ex.Message}");
            throw;
        }
    }
}
