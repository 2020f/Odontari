using System.Globalization;
using Odontari.Web.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Odontari.Web.Services;

/// <summary>Genera PDF del Timeline (histórico) del histograma para un paciente y rango de fechas.</summary>
public class HistogramaExportService
{
    private static readonly CultureInfo Ci = CultureInfo.GetCultureInfo("es-DO");

    /// <summary>Genera un PDF con la sección Timeline (histórico): paciente, rango de fechas y tabla de eventos.</summary>
    public byte[] GenerateTimelinePdf(string nombrePaciente, string? apellidosPaciente, DateTime? fechaInicio, DateTime? fechaFin, List<HistorialEventoViewModel> eventos)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var nombreCompleto = string.IsNullOrWhiteSpace(apellidosPaciente) ? nombrePaciente : $"{nombrePaciente} {apellidosPaciente}".Trim();
        var rangoTexto = fechaInicio.HasValue && fechaFin.HasValue
            ? $"{fechaInicio.Value:dd/MM/yyyy} — {fechaFin.Value:dd/MM/yyyy}"
            : fechaInicio.HasValue
                ? $"Desde {fechaInicio.Value:dd/MM/yyyy}"
                : fechaFin.HasValue
                    ? $"Hasta {fechaFin.Value:dd/MM/yyyy}"
                    : "Todos los registros";

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text("Timeline (histórico)").Bold().FontSize(14);
                    col.Item().Text($"Paciente: {nombreCompleto}").FontSize(10);
                    col.Item().Text($"Rango de fechas: {rangoTexto}").FontSize(9);
                    col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).Italic();
                });

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Item().Text("Procedimientos y eventos realizados").Bold().FontSize(11);
                    col.Item().PaddingBottom(6);

                    if (eventos.Count == 0)
                    {
                        col.Item().Text("No hay eventos en el rango seleccionado.").Italic();
                        return;
                    }

                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(55);  // Fecha
                            c.RelativeColumn(1);   // Tipo de evento
                            c.RelativeColumn(2);   // Descripción
                        });
                        t.Header(h =>
                        {
                            h.Cell().Element(HeaderStyle).Text("Fecha");
                            h.Cell().Element(HeaderStyle).Text("Tipo de evento");
                            h.Cell().Element(HeaderStyle).Text("Descripción");
                        });
                        foreach (var e in eventos)
                        {
                            var desc = (e.Descripcion ?? "—").Replace("\n", " ");
                            t.Cell().Element(CellStyle).Text(e.FechaEvento.ToString("dd/MM/yyyy HH:mm", Ci));
                            t.Cell().Element(CellStyle).Text(e.TipoEvento ?? "—");
                            t.Cell().Element(CellStyle).Text(desc);
                        }
                    });
                });
            });
        });

        return doc.GeneratePdf();
    }

    private static IContainer CellStyle(IContainer c) =>
        c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4);

    private static IContainer HeaderStyle(IContainer c) =>
        c.DefaultTextStyle(x => x.Bold()).BorderBottom(1).BorderColor(Colors.Grey.Medium).PaddingVertical(4).PaddingHorizontal(4);
}
