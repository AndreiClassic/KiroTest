using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace backend.Services;

public class PdfService : IPdfService
{
    public PdfService()
    {
        // Configure QuestPDF license (Community license for non-commercial use)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GeneratePropertyReport(int lotId, Models.Lot.LotData lotData, Models.InsuranceResponse? insuranceQuote)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header()
                    .AlignCenter()
                    .Column(column =>
                    {
                        column.Item().Text("PROPERTY REPORT")
                            .FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                        column.Item().Text($"LOT {lotId}")
                            .FontSize(16).SemiBold();
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(15);

                        // Lot Information Section
                        column.Item().Element(c => RenderSection(c, "LOT INFORMATION", (content) =>
                        {
                            content.Column(col =>
                            {
                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text($"Site Area: {lotData.LandArea:F0}m²");
                                    row.RelativeItem().Text($"Floor Area: {lotData.FloorArea:F1}m²");
                                });
                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text($"Bedrooms: {lotData.Bedrooms}");
                                    row.RelativeItem().Text($"Bathrooms: {lotData.Bathrooms}");
                                });
                                col.Item().Text($"Build Type: {lotData.BuildType}");
                                if (lotData.BuildYear.HasValue)
                                {
                                    col.Item().Text($"Build Year: {lotData.BuildYear}");
                                }
                            });
                        }));

                        // Address Section
                        column.Item().Element(c => RenderSection(c, "ADDRESS", (content) =>
                        {
                            content.Column(col =>
                            {
                                col.Item().Text(lotData.Address);
                                col.Item().Text($"{lotData.Location}, {lotData.Region}");
                                if (lotData.Latitude.HasValue && lotData.Longitude.HasValue)
                                {
                                    col.Item().Text($"Coordinates: {lotData.Latitude:F6}, {lotData.Longitude:F6}");
                                }
                            });
                        }));

                        // Hazard Zones Table
                        column.Item().Element(c => RenderSection(c, "HAZARD ZONES", (content) =>
                        {
                            content.Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                });

                                // Header
                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten3)
                                        .Padding(5).Text("ZONE TYPE").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3)
                                        .Padding(5).Text("RISK LEVEL").Bold();
                                });

                                // Flood Zone - default to "Low" if null/empty/unknown
                                var floodZone = string.IsNullOrWhiteSpace(lotData.FloodZone) || 
                                               lotData.FloodZone.Equals("Unknown", StringComparison.OrdinalIgnoreCase)
                                    ? "Low" 
                                    : lotData.FloodZone;
                                table.Cell().Padding(5).Text("Flood Zone");
                                table.Cell().Padding(5).Text(floodZone.ToUpper())
                                    .Bold().FontColor(GetRiskColor(floodZone));

                                // Earthquake Zone - default to "Low" if null/empty/unknown
                                var earthquakeZone = string.IsNullOrWhiteSpace(lotData.EarthquakeZone) || 
                                                    lotData.EarthquakeZone.Equals("Unknown", StringComparison.OrdinalIgnoreCase)
                                    ? "Low" 
                                    : lotData.EarthquakeZone;
                                table.Cell().Padding(5).Text("Earthquake Zone");
                                table.Cell().Padding(5).Text(earthquakeZone.ToUpper())
                                    .Bold().FontColor(GetRiskColor(earthquakeZone));

                                // Wind Zone
                                table.Cell().Padding(5).Text("Wind Zone");
                                table.Cell().Padding(5).Text("HIGH")
                                    .Bold().FontColor(Colors.Red.Darken2);

                                // Climate Zone
                                table.Cell().Padding(5).Text("Climate Zone");
                                table.Cell().Padding(5).Text("4");
                            });
                        }));

                        // Site Coverage Section
                        column.Item().Element(c => RenderSection(c, "SITE COVERAGE", (content) =>
                        {
                            var coverage = (lotData.FloorArea / lotData.LandArea * 100);
                            content.Column(col =>
                            {
                                col.Item().Text($"Dwelling (O/Cladding): {lotData.FloorArea:F1}m²");
                                col.Item().Text($"Site Area: {lotData.LandArea:F0}m²");
                                col.Item().Text($"Coverage: {coverage:F1}% (Max 50%)");
                            });
                        }));

                        // Insurance Quote Section (if available)
                        if (insuranceQuote != null)
                        {
                            column.Item().Element(c => RenderSection(c, "INSURANCE QUOTE", (content) =>
                            {
                                // Default risk level to "Low" if null or empty
                                var riskLevel = string.IsNullOrWhiteSpace(insuranceQuote.RiskLevel) 
                                    ? "Low" 
                                    : insuranceQuote.RiskLevel;
                                
                                content.Background(Colors.Blue.Lighten4)
                                    .Padding(10)
                                    .Column(col =>
                                    {
                                        col.Item().Row(row =>
                                        {
                                            row.RelativeItem().Text("Annual Premium:");
                                            row.RelativeItem().Text($"${insuranceQuote.AnnualPremium:F2} NZD").Bold();
                                        });
                                        col.Item().Row(row =>
                                        {
                                            row.RelativeItem().Text("Monthly Premium:");
                                            row.RelativeItem().Text($"${insuranceQuote.MonthlyPremium:F2} NZD").Bold();
                                        });
                                        col.Item().Row(row =>
                                        {
                                            row.RelativeItem().Text("Risk Level:");
                                            row.RelativeItem().Text(riskLevel.ToUpper())
                                                .Bold().FontColor(GetRiskColor(riskLevel));
                                        });
                                    });
                            }));
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}").FontSize(8).Italic();
                        text.Line("");
                        text.Span("NZ House Insurance Calculator").FontSize(8).Italic();
                    });
            });
        });

        return document.GeneratePdf();
    }

    private void RenderSection(IContainer container, string title, Action<IContainer> content)
    {
        container.Column(column =>
        {
            column.Item().Text(title).FontSize(12).Bold().FontColor(Colors.Blue.Darken1);
            column.Item().PaddingTop(5).Element(content);
        });
    }

    private string GetRiskColor(string riskLevel)
    {
        return riskLevel.ToLower() switch
        {
            "high" => Colors.Red.Darken2,
            "medium" => Colors.Orange.Darken1,
            "low" => Colors.Green.Darken1,
            _ => Colors.Grey.Darken1
        };
    }
}
