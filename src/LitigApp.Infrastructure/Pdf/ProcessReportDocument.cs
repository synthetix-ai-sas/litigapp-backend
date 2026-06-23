using System.Globalization;
using LitigApp.Application.Features.Processes.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LitigApp.Infrastructure.Pdf;

/// <summary>
/// QuestPDF document for a single process report: header, metadata, subjects and actions.
/// </summary>
internal sealed class ProcessReportDocument(ProcessDetailDto process) : IDocument
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("es-CO");

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken4));

            page.Header().Element(ComposeHeader);
            page.Content().PaddingVertical(10).Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text("Reporte de Proceso Judicial")
                .FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
            col.Item().Text(process.FileNumber).FontSize(12).SemiBold();
            if (!string.IsNullOrWhiteSpace(process.Alias))
                col.Item().Text(process.Alias!).FontSize(10).Italic().FontColor(Colors.Grey.Darken1);
            col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(14);
            col.Item().Element(ComposeMetadata);
            col.Item().Element(ComposeSubjects);
            col.Item().Element(ComposeActions);
        });
    }

    private void ComposeMetadata(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text("Información general").FontSize(12).Bold();
            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(c => { c.ConstantColumn(150); c.RelativeColumn(); });

                void Row(string label, string? value)
                {
                    table.Cell().PaddingVertical(2).Text(label).SemiBold();
                    table.Cell().PaddingVertical(2).Text(string.IsNullOrWhiteSpace(value) ? "—" : value);
                }

                Row("Despacho", process.Court?.Name);
                Row("Ciudad", process.Court?.CityName);
                Row("Departamento", process.Court?.DepartmentName);
                Row("Año de radicación", process.FilingYear?.ToString(Culture));
                Row("Tipo de proceso", process.ProcessType);
                Row("Clase de proceso", process.ProcessClass);
                Row("Ponente / Juez", process.JudgeName);
                Row("Estado actual", process.CurrentStatus);
                Row("Última actuación", process.LastCourtActionAt?.ToString("yyyy-MM-dd", Culture));
            });
        });
    }

    private void ComposeSubjects(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text($"Sujetos procesales ({process.Subjects.Count})").FontSize(12).Bold();

            if (process.Subjects.Count == 0)
            {
                col.Item().PaddingTop(4).Text("Sin sujetos registrados.").FontColor(Colors.Grey.Darken1);
                return;
            }

            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(c => { c.ConstantColumn(120); c.RelativeColumn(); c.ConstantColumn(120); });
                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Tipo");
                    header.Cell().Element(HeaderCell).Text("Nombre");
                    header.Cell().Element(HeaderCell).Text("Identificación");
                });

                foreach (var s in process.Subjects)
                {
                    table.Cell().Element(BodyCell).Text(s.Type);
                    table.Cell().Element(BodyCell).Text(s.Name);
                    table.Cell().Element(BodyCell).Text(string.IsNullOrWhiteSpace(s.Identification) ? "—" : s.Identification!);
                }
            });
        });
    }

    private void ComposeActions(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text($"Actuaciones ({process.Actions.Count})").FontSize(12).Bold();

            if (process.Actions.Count == 0)
            {
                col.Item().PaddingTop(4).Text("Sin actuaciones registradas.").FontColor(Colors.Grey.Darken1);
                return;
            }

            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(45);
                    c.ConstantColumn(75);
                    c.RelativeColumn(2);
                    c.RelativeColumn(3);
                });
                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Consec.");
                    header.Cell().Element(HeaderCell).Text("Fecha");
                    header.Cell().Element(HeaderCell).Text("Actuación");
                    header.Cell().Element(HeaderCell).Text("Anotación");
                });

                foreach (var a in process.Actions)
                {
                    table.Cell().Element(BodyCell).Text(a.ConsecutiveNumber.ToString(Culture));
                    table.Cell().Element(BodyCell).Text(a.ActionDate?.ToString("yyyy-MM-dd", Culture) ?? "—");
                    table.Cell().Element(BodyCell).Text(string.IsNullOrWhiteSpace(a.Action) ? "—" : a.Action!);
                    table.Cell().Element(BodyCell).Text(string.IsNullOrWhiteSpace(a.Annotation) ? "—" : a.Annotation!);
                }
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("Generado por LitigApp — ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    t.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'", Culture)).FontSize(8).FontColor(Colors.Grey.Darken1);
                });
                row.ConstantItem(80).AlignRight().Text(t =>
                {
                    t.Span("Página ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    t.CurrentPageNumber().FontSize(8);
                    t.Span(" / ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    t.TotalPages().FontSize(8);
                });
            });
        });
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4)
            .BorderBottom(1).BorderColor(Colors.Grey.Medium).DefaultTextStyle(x => x.SemiBold());

    private static IContainer BodyCell(IContainer container) =>
        container.PaddingVertical(3).PaddingHorizontal(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);
}
