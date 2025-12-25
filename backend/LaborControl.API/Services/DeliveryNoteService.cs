using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using LaborControl.API.Models;

namespace LaborControl.API.Services;

public interface IDeliveryNoteService
{
    byte[] GenerateDeliveryNotePdf(Order order);
}

public class DeliveryNoteService : IDeliveryNoteService
{
    private readonly ILogger<DeliveryNoteService> _logger;
    private readonly string? _logoPath;

    public DeliveryNoteService(ILogger<DeliveryNoteService> logger, IConfiguration configuration)
    {
        _logger = logger;

        // Configurer QuestPDF pour utiliser la licence communautaire
        QuestPDF.Settings.License = LicenseType.Community;

        // Définir le chemin du logo s'il existe
        var webRootPath = configuration["WebRootPath"] ?? "wwwroot";
        var logoPath = Path.Combine(webRootPath, "logo-lc.png");
        _logoPath = File.Exists(logoPath) ? logoPath : null;
    }

    public byte[] GenerateDeliveryNotePdf(Order order)
    {
        try
        {
            if (order.Customer == null)
            {
                throw new InvalidOperationException("Les informations du client sont requises pour générer le bon de livraison");
            }

            if (string.IsNullOrEmpty(order.PackagingCode))
            {
                throw new InvalidOperationException("Le code packaging est requis pour générer le bon de livraison");
            }

            // Générer le PDF et le retourner comme tableau de bytes
            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header()
                        .Height(140)
                        .Padding(15)
                        .Column(column =>
                        {
                            // Logo et titre
                            column.Item().Row(row =>
                            {
                                // Logo à gauche (si disponible)
                                if (_logoPath != null)
                                {
                                    row.ConstantItem(80).AlignMiddle().Image(_logoPath);
                                }

                                // Titre au centre
                                row.RelativeItem().PaddingLeft(_logoPath != null ? 15 : 0).AlignMiddle().Column(titleColumn =>
                                {
                                    // Titre "LABOR CONTROL" en 2 couleurs
                                    titleColumn.Item().Text(text =>
                                    {
                                        text.Span("LABOR").FontSize(26).Bold().FontColor("#22D3EE"); // cyan-400
                                        text.Span(" CONTROL").FontSize(26).Bold().FontColor("#1E3A8A"); // blue-800
                                    });

                                    titleColumn.Item().Text("Système de gestion NFC")
                                        .FontSize(10)
                                        .FontColor(Colors.Grey.Darken2);
                                });
                            });

                            // Séparateur
                            column.Item().PaddingTop(10).LineHorizontal(2).LineColor("#22D3EE");

                            // Titre "BON DE LIVRAISON"
                            column.Item().PaddingTop(10).AlignCenter().Text("BON DE LIVRAISON")
                                .FontSize(18)
                                .Bold()
                                .FontColor(Colors.Blue.Darken2);
                        });

                    page.Content()
                        .PaddingVertical(10)
                        .Column(column =>
                        {
                            // Informations commande et date
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Column(leftColumn =>
                                {
                                    leftColumn.Item().Text(text =>
                                    {
                                        text.Span("Commande N° : ").FontSize(11).SemiBold();
                                        text.Span(order.OrderNumber).FontSize(11).Bold().FontColor(Colors.Blue.Darken1);
                                    });

                                    leftColumn.Item().PaddingTop(5).Text($"Date d'expédition : {DateTime.Now:dd/MM/yyyy}")
                                        .FontSize(10)
                                        .FontColor(Colors.Grey.Darken1);
                                });

                                row.RelativeItem().AlignRight().Column(rightColumn =>
                                {
                                    if (!string.IsNullOrEmpty(order.TrackingNumber))
                                    {
                                        rightColumn.Item().Text(text =>
                                        {
                                            text.Span("N° de suivi : ").FontSize(10).SemiBold();
                                            text.Span(order.TrackingNumber).FontSize(10).Bold();
                                        });
                                    }
                                });
                            });

                            // Espace
                            column.Item().PaddingTop(20);

                            // Informations destinataire
                            column.Item()
                                .Border(1)
                                .BorderColor(Colors.Grey.Lighten1)
                                .Background(Colors.Grey.Lighten3)
                                .Padding(15)
                                .Column(destColumn =>
                                {
                                    destColumn.Item().Text("DESTINATAIRE")
                                        .FontSize(12)
                                        .Bold()
                                        .FontColor(Colors.Blue.Darken2);

                                    destColumn.Item().PaddingTop(10).Text(order.Customer.Name)
                                        .FontSize(12)
                                        .Bold();

                                    if (!string.IsNullOrEmpty(order.DeliveryAddress))
                                    {
                                        destColumn.Item().PaddingTop(5).Text(order.DeliveryAddress)
                                            .FontSize(10);
                                    }

                                    if (!string.IsNullOrEmpty(order.DeliveryPostalCode) || !string.IsNullOrEmpty(order.DeliveryCity))
                                    {
                                        destColumn.Item().Text($"{order.DeliveryPostalCode} {order.DeliveryCity}".Trim())
                                            .FontSize(10);
                                    }

                                    if (!string.IsNullOrEmpty(order.DeliveryCountry))
                                    {
                                        destColumn.Item().Text(order.DeliveryCountry)
                                            .FontSize(10);
                                    }
                                });

                            // Espace
                            column.Item().PaddingTop(25);

                            // Contenu de la livraison
                            column.Item().Text("CONTENU DE LA LIVRAISON")
                                .FontSize(12)
                                .Bold()
                                .FontColor(Colors.Blue.Darken2);

                            column.Item().PaddingTop(10)
                                .Border(1)
                                .BorderColor(Colors.Grey.Lighten1)
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(1);
                                    });

                                    // En-tête
                                    table.Cell().Background(Colors.Grey.Lighten2).Padding(10).Text("Article").FontSize(10).Bold();
                                    table.Cell().Background(Colors.Grey.Lighten2).Padding(10).AlignCenter().Text("Quantité").FontSize(10).Bold();

                                    // Contenu
                                    table.Cell().Padding(10).Text("Puces RFID NFC Labor Control").FontSize(10);
                                    table.Cell().Padding(10).AlignCenter().Text(order.ChipsQuantity.ToString()).FontSize(10).Bold();
                                });

                            // Espace
                            column.Item().PaddingTop(30);

                            // CODE PACKAGING - Section très importante
                            column.Item()
                                .Border(3)
                                .BorderColor("#22D3EE") // cyan
                                .Background("#F0F9FF") // bleu très clair
                                .Padding(20)
                                .Column(pkgColumn =>
                                {
                                    pkgColumn.Item().AlignCenter().Text("CODE PACKAGING")
                                        .FontSize(14)
                                        .Bold()
                                        .FontColor("#1E3A8A"); // blue-800

                                    pkgColumn.Item().PaddingTop(10).AlignCenter()
                                        .Border(2)
                                        .BorderColor("#1E3A8A")
                                        .Background(Colors.White)
                                        .Padding(15)
                                        .Text(order.PackagingCode)
                                        .FontSize(24)
                                        .Bold()
                                        .FontColor("#DC2626"); // red-600

                                    pkgColumn.Item().PaddingTop(15).AlignCenter()
                                        .PaddingHorizontal(30)
                                        .Text("⚠️ IMPORTANT : Veuillez saisir ce code dans votre espace client pour confirmer la réception de votre commande et commencer à activer vos puces RFID.")
                                        .FontSize(10)
                                        .Bold()
                                        .FontColor("#DC2626") // red-600
                                        .LineHeight(1.5f);

                                    pkgColumn.Item().PaddingTop(10).AlignCenter()
                                        .Text("Sans ce code, vous ne pourrez pas activer vos puces.")
                                        .FontSize(9)
                                        .Italic()
                                        .FontColor(Colors.Grey.Darken2);
                                });

                            // Espace
                            column.Item().PaddingTop(25);

                            // Instructions
                            column.Item()
                                .Border(1)
                                .BorderColor(Colors.Grey.Lighten1)
                                .Background(Colors.Grey.Lighten3)
                                .Padding(15)
                                .Column(instrColumn =>
                                {
                                    instrColumn.Item().Text("INSTRUCTIONS")
                                        .FontSize(11)
                                        .Bold()
                                        .FontColor(Colors.Blue.Darken2);

                                    instrColumn.Item().PaddingTop(8).Text("1. Connectez-vous à votre espace client sur gestion.labor-control.fr")
                                        .FontSize(9)
                                        .LineHeight(1.4f);

                                    instrColumn.Item().Text("2. Accédez à la section \"Mes Commandes\"")
                                        .FontSize(9)
                                        .LineHeight(1.4f);

                                    instrColumn.Item().Text("3. Saisissez le code packaging ci-dessus pour confirmer la réception")
                                        .FontSize(9)
                                        .LineHeight(1.4f);

                                    instrColumn.Item().Text("4. Vos puces seront alors disponibles pour activation")
                                        .FontSize(9)
                                        .LineHeight(1.4f);
                                });
                        });

                    page.Footer()
                        .Height(50)
                        .AlignCenter()
                        .Column(footerColumn =>
                        {
                            footerColumn.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                            footerColumn.Item().PaddingTop(10).Text(text =>
                            {
                                text.Span("LABOR CONTROL").Bold().FontSize(9);
                                text.Span(" | Contact : contact@labor-control.fr | ").FontSize(8).FontColor(Colors.Grey.Darken1);
                                text.Span("www.labor-control.fr").FontSize(8).FontColor(Colors.Blue.Medium);
                            });
                        });
                });
            }).GeneratePdf();

            _logger.LogInformation($"Bon de livraison généré pour la commande {order.OrderNumber}");
            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erreur lors de la génération du bon de livraison pour la commande {order.OrderNumber}");
            throw;
        }
    }
}
