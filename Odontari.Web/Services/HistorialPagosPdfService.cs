using System.Globalization;
using Odontari.Web.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Odontari.Web.Services;

/// <summary>Genera PDF del historial de pagos (filtrado).</summary>
public class HistorialPagosPdfService
{
    private static readonly CultureInfo Ci = CultureInfo.GetCultureInfo("es-DO");

    public byte[] GeneratePdf(List<HistorialPagoItemViewModel> items, DateTime? desde, DateTime? hasta, string? metodoPago = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var titulo = "Historial de pagos";
        var filtros = "";
        if (desde.HasValue) filtros += $"Desde: {desde.Value:dd/MM/yyyy} ";
        if (hasta.HasValue) filtros += $"Hasta: {hasta.Value:dd/MM/yyyy} ";
        if (!string.IsNullOrWhiteSpace(metodoPago)) filtros += $"Método: {metodoPago}";

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text(titulo).Bold().FontSize(14);
                    if (!string.IsNullOrWhiteSpace(filtros)) col.Item().Text(filtros).FontSize(9);
                    col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).Italic();
                });

                page.Content().PaddingVertical(10).Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(55);
                        c.RelativeColumn(2);
                        c.ConstantColumn(60);
                        c.ConstantColumn(65);
                        c.ConstantColumn(85);
                        c.RelativeColumn(1);
                    });
                    t.Header(h =>
                    {
                        h.Cell().Element(HeaderStyle).Text("Fecha");
                        h.Cell().Element(HeaderStyle).Text("Paciente");
                        h.Cell().Element(HeaderStyle).AlignRight().Text("Orden total");
                        h.Cell().Element(HeaderStyle).AlignRight().Text("Monto");
                        h.Cell().Element(HeaderStyle).Text("Método de pago");
                        h.Cell().Element(HeaderStyle).Text("Referencia");
                    });
                    foreach (var p in items)
                    {
                        t.Cell().Element(CellStyle).Text(p.FechaPago.ToString("dd/MM/yyyy HH:mm", Ci));
                        t.Cell().Element(CellStyle).Text(p.PacienteNombre ?? "—");
                        t.Cell().Element(CellStyle).AlignRight().Text(p.OrdenTotal.ToString("N2", Ci));
                        t.Cell().Element(CellStyle).AlignRight().Text(p.Monto.ToString("N2", Ci));
                        t.Cell().Element(CellStyle).Text(p.MetodoPago ?? "—");
                        t.Cell().Element(CellStyle).Text((p.Referencia ?? "—").Replace("\n", " "));
                    }
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
