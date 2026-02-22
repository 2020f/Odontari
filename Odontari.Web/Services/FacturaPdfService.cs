using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Odontari.Web.Services;

/// <summary>Datos para generar el PDF de una factura.</summary>
public class FacturaPdfData
{
    public string RazonSocial { get; set; } = "";
    public string? RNC { get; set; }
    public string? DireccionFiscal { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public int NumeroInterno { get; set; }
    public string? NCF { get; set; }
    public bool EsFiscal { get; set; }
    public DateTime FechaEmision { get; set; }
    public string ClienteNombre { get; set; } = "";
    public string? ClienteRNC { get; set; }
    public string? ClienteCedula { get; set; }
    public List<FacturaLineaPdf> Lineas { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Itbis { get; set; }
    public decimal Total { get; set; }
    public string? FormaPago { get; set; }
    public string? MensajeFactura { get; set; }
    public string? CondicionesPago { get; set; }
}

public class FacturaLineaPdf
{
    public string Descripcion { get; set; } = "";
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

/// <summary>Genera el PDF de una factura (interna o fiscal).</summary>
public class FacturaPdfService
{
    private static readonly CultureInfo Ci = CultureInfo.GetCultureInfo("es-DO");

    public byte[] GeneratePdf(FacturaPdfData data)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text(data.RazonSocial).Bold().FontSize(14);
                    if (!string.IsNullOrEmpty(data.RNC)) col.Item().Text($"RNC: {data.RNC}").FontSize(9);
                    if (!string.IsNullOrEmpty(data.DireccionFiscal)) col.Item().Text(data.DireccionFiscal).FontSize(8);
                    if (!string.IsNullOrEmpty(data.Telefono)) col.Item().Text($"Tel: {data.Telefono}").FontSize(8);
                    if (!string.IsNullOrEmpty(data.Email)) col.Item().Text(data.Email).FontSize(8);
                    col.Item().PaddingBottom(8);
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("FACTURA").Bold().FontSize(12);
                            c.Item().Text($"Nº {data.NumeroInterno}").FontSize(10);
                            if (!string.IsNullOrEmpty(data.NCF)) c.Item().Text($"NCF: {data.NCF}").FontSize(9);
                            if (!data.EsFiscal) c.Item().Text("Documento no válido para crédito fiscal").FontSize(8).Italic();
                            c.Item().Text($"Fecha: {data.FechaEmision:dd/MM/yyyy}").FontSize(9);
                        });
                    });
                });

                page.Content().PaddingVertical(8).Column(col =>
                {
                    col.Item().Text("Cliente").Bold().FontSize(10);
                    col.Item().Text(data.ClienteNombre).FontSize(9);
                    if (!string.IsNullOrEmpty(data.ClienteRNC)) col.Item().Text($"RNC: {data.ClienteRNC}");
                    if (!string.IsNullOrEmpty(data.ClienteCedula)) col.Item().Text($"Cédula: {data.ClienteCedula}");
                    col.Item().PaddingBottom(10);

                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.ConstantColumn(50);
                            c.ConstantColumn(60);
                            c.ConstantColumn(70);
                        });
                        t.Header(h =>
                        {
                            h.Cell().Element(HeaderStyle).Text("Descripción");
                            h.Cell().Element(HeaderStyle).AlignRight().Text("Cant.");
                            h.Cell().Element(HeaderStyle).AlignRight().Text("P. unit.");
                            h.Cell().Element(HeaderStyle).AlignRight().Text("Subtotal");
                        });
                        foreach (var l in data.Lineas)
                        {
                            t.Cell().Element(CellStyle).Text(l.Descripcion);
                            t.Cell().Element(CellStyle).AlignRight().Text(l.Cantidad.ToString("N2", Ci));
                            t.Cell().Element(CellStyle).AlignRight().Text(l.PrecioUnitario.ToString("N2", Ci));
                            t.Cell().Element(CellStyle).AlignRight().Text(l.Subtotal.ToString("N2", Ci));
                        }
                    });

                    col.Item().PaddingTop(12).AlignRight().Width(180).Column(c =>
                    {
                        c.Item().Row(r => { r.RelativeItem().Text("Subtotal:"); r.RelativeItem().AlignRight().Text(data.Subtotal.ToString("N2", Ci)); });
                        if (data.Itbis > 0) c.Item().Row(r => { r.RelativeItem().Text("ITBIS:"); r.RelativeItem().AlignRight().Text(data.Itbis.ToString("N2", Ci)); });
                        c.Item().Row(r => { r.RelativeItem().Text("Total:").Bold(); r.RelativeItem().AlignRight().Text(data.Total.ToString("N2", Ci)).Bold(); });
                    });
                    col.Item().PaddingTop(6).Text($"Forma de pago: {data.FormaPago ?? "—"}").FontSize(9);
                    if (!string.IsNullOrEmpty(data.MensajeFactura)) col.Item().PaddingTop(8).Text(data.MensajeFactura).Italic().FontSize(8);
                    if (!string.IsNullOrEmpty(data.CondicionesPago)) col.Item().Text(data.CondicionesPago).FontSize(8);
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
