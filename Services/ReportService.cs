using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RacingTelemetryAnalyzer.Models;
using System.IO;

namespace RacingTelemetryAnalyzer.Services;

public class ReportService
{
    public byte[] GeneratePdfReport(Lap lap, System.Collections.Generic.IEnumerable<AnalysisResult> analysisResults)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header()
                    .Text("Telemetry Analysis Report")
                    .SemiBold().FontSize(24).FontColor(Colors.Blue.Darken2);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(x =>
                    {
                        x.Spacing(20);
                        
                        x.Item().Text($"Lap Number: {lap.LapNumber}");
                        x.Item().Text($"Lap Time: {lap.LapTime}");

                        x.Item().Text("Insights & Recommendations").SemiBold().FontSize(18);

                        foreach (var result in analysisResults)
                        {
                            x.Item().Text($"- [{result.RecommendationType}] {result.Recommendation}");
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
            });
        });

        using var ms = new MemoryStream();
        document.GeneratePdf(ms);
        return ms.ToArray();
    }
}
