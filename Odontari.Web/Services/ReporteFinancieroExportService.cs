using System.Globalization;
using ClosedXML.Excel;
using Odontari.Web.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Odontari.Web.Services;

/// <summary>Genera Excel y PDF del reporte financiero con la estructura formal requerida.</summary>
public class ReporteFinancieroExportService
{
    private static readonly CultureInfo Ci = CultureInfo.GetCultureInfo("es-ES");

    public byte[] GenerateExcel(ReporteFinancieroData data)
    {
        using var workbook = new XLWorkbook();

        // Hoja 1: Resumen general
        var wsResumen = workbook.Worksheets.Add("Resumen general");
        WriteEncabezado(wsResumen, data.Encabezado, 1);
        int row = 8;
        wsResumen.Cell(row, 1).Value = "RESUMEN FINANCIERO";
        row++;
        wsResumen.Cell(row, 1).Value = "Total Facturado"; wsResumen.Cell(row, 2).Value = data.Resumen.TotalFacturado; row++;
        wsResumen.Cell(row, 1).Value = "Total Cobrado"; wsResumen.Cell(row, 2).Value = data.Resumen.TotalCobrado; row++;
        wsResumen.Cell(row, 1).Value = "Total Pendiente"; wsResumen.Cell(row, 2).Value = data.Resumen.TotalPendiente; row++;
        wsResumen.Cell(row, 1).Value = "Total Anulado"; wsResumen.Cell(row, 2).Value = data.Resumen.TotalAnulado; row++;
        wsResumen.Cell(row, 1).Value = "Descuentos aplicados"; wsResumen.Cell(row, 2).Value = data.Resumen.DescuentosAplicados; row++;
        wsResumen.Cell(row, 1).Value = "Total Neto Real"; wsResumen.Cell(row, 2).Value = data.Resumen.TotalNetoReal; row++;
        wsResumen.Column(2).Style.NumberFormat.Format = "#,##0.00";
        row += 2;
        wsResumen.Cell(row, 1).Value = "TRATAMIENTOS MÁS VENDIDOS"; row++;
        wsResumen.Cell(row, 1).Value = "Tratamiento"; wsResumen.Cell(row, 2).Value = "Cantidad"; wsResumen.Cell(row, 3).Value = "Total generado"; wsResumen.Row(row).Style.Font.Bold = true; row++;
        foreach (var t in data.TratamientosMasVendidos.Take(15))
        {
            wsResumen.Cell(row, 1).Value = t.Tratamiento; wsResumen.Cell(row, 2).Value = t.Cantidad; wsResumen.Cell(row, 3).Value = t.TotalGenerado; row++;
        }
        wsResumen.Column(3).Style.NumberFormat.Format = "#,##0.00";

        // Hoja 2: Detalle de ingresos
        var wsDetalle = workbook.Worksheets.Add("Detalle de ingresos");
        WriteEncabezado(wsDetalle, data.Encabezado, 1);
        row = 8;
        wsDetalle.Cell(row, 1).Value = "Fecha"; wsDetalle.Cell(row, 2).Value = "Nº Cita"; wsDetalle.Cell(row, 3).Value = "Paciente"; wsDetalle.Cell(row, 4).Value = "Doctor";
        wsDetalle.Cell(row, 5).Value = "Tratamiento"; wsDetalle.Cell(row, 6).Value = "Método pago"; wsDetalle.Cell(row, 7).Value = "Monto total"; wsDetalle.Cell(row, 8).Value = "Monto pagado"; wsDetalle.Cell(row, 9).Value = "Saldo pendiente"; wsDetalle.Cell(row, 10).Value = "Estado";
        wsDetalle.Row(row).Style.Font.Bold = true;
        row++;
        foreach (var r in data.DetalleIngresos)
        {
            wsDetalle.Cell(row, 1).Value = r.Fecha.ToString("dd/MM/yyyy", Ci);
            wsDetalle.Cell(row, 2).Value = r.NumeroCita;
            wsDetalle.Cell(row, 3).Value = r.Paciente;
            wsDetalle.Cell(row, 4).Value = r.Doctor;
            wsDetalle.Cell(row, 5).Value = r.Tratamiento;
            wsDetalle.Cell(row, 6).Value = r.MetodoPago;
            wsDetalle.Cell(row, 7).Value = r.MontoTotal;
            wsDetalle.Cell(row, 8).Value = r.MontoPagado;
            wsDetalle.Cell(row, 9).Value = r.SaldoPendiente;
            wsDetalle.Cell(row, 10).Value = r.Estado;
            row++;
        }
        wsDetalle.Columns(7, 9).Style.NumberFormat.Format = "#,##0.00";

        // Hoja 3: Cuentas por cobrar
        var wsCxC = workbook.Worksheets.Add("Cuentas por cobrar");
        WriteEncabezado(wsCxC, data.Encabezado, 1);
        row = 8;
        wsCxC.Cell(row, 1).Value = "Paciente"; wsCxC.Cell(row, 2).Value = "Total pendiente"; wsCxC.Cell(row, 3).Value = "Última fecha atención"; wsCxC.Cell(row, 4).Value = "Días vencidos";
        wsCxC.Row(row).Style.Font.Bold = true;
        row++;
        foreach (var r in data.CuentasPorCobrar)
        {
            wsCxC.Cell(row, 1).Value = r.Paciente;
            wsCxC.Cell(row, 2).Value = r.TotalPendiente;
            wsCxC.Cell(row, 3).Value = r.UltimaFechaAtencion?.ToString("dd/MM/yyyy", Ci) ?? "";
            wsCxC.Cell(row, 4).Value = r.DiasVencidos.HasValue ? r.DiasVencidos.Value : "";
            row++;
        }
        wsCxC.Column(2).Style.NumberFormat.Format = "#,##0.00";

        // Hoja 4: Producción por doctor
        var wsDoctor = workbook.Worksheets.Add("Producción por doctor");
        WriteEncabezado(wsDoctor, data.Encabezado, 1);
        row = 8;
        wsDoctor.Cell(row, 1).Value = "Doctor"; wsDoctor.Cell(row, 2).Value = "Total facturado"; wsDoctor.Cell(row, 3).Value = "Total cobrado"; wsDoctor.Cell(row, 4).Value = "Pacientes atendidos";
        wsDoctor.Row(row).Style.Font.Bold = true;
        row++;
        foreach (var r in data.ProduccionPorDoctor)
        {
            wsDoctor.Cell(row, 1).Value = r.Doctor;
            wsDoctor.Cell(row, 2).Value = r.TotalFacturado;
            wsDoctor.Cell(row, 3).Value = r.TotalCobrado;
            wsDoctor.Cell(row, 4).Value = r.CantidadPacientesAtendidos;
            row++;
        }
        wsDoctor.Columns(2, 3).Style.NumberFormat.Format = "#,##0.00";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static void WriteEncabezado(IXLWorksheet ws, EncabezadoReporte enc, int startRow)
    {
        ws.Cell(startRow, 1).Value = enc.NombreClinica; ws.Row(startRow).Style.Font.Bold = true; startRow++;
        ws.Cell(startRow, 1).Value = "RNC: " + enc.RNC; startRow++;
        ws.Cell(startRow, 1).Value = enc.Direccion; startRow++;
        ws.Cell(startRow, 1).Value = enc.Telefono; startRow++;
        ws.Cell(startRow, 1).Value = "Fecha de generación: " + enc.FechaGeneracion.ToString("dd/MM/yyyy HH:mm", Ci); startRow++;
        ws.Cell(startRow, 1).Value = "Rango: " + enc.RangoFechas; startRow++;
        ws.Cell(startRow, 1).Value = "Generado por: " + enc.UsuarioGenero; startRow++;
    }

    public byte[] GeneratePdf(ReporteFinancieroData data)
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
                    col.Item().Text(data.Encabezado.NombreClinica).Bold().FontSize(14);
                    col.Item().Text("RNC: " + data.Encabezado.RNC);
                    col.Item().Text(data.Encabezado.Direccion);
                    col.Item().Text(data.Encabezado.Telefono);
                    col.Item().PaddingVertical(4).Text("Reporte Financiero").Bold().FontSize(12);
                    col.Item().Text("Rango: " + data.Encabezado.RangoFechas + "  |  Generado: " + data.Encabezado.FechaGeneracion.ToString("dd/MM/yyyy HH:mm", Ci) + "  |  Por: " + data.Encabezado.UsuarioGenero).FontSize(8);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    // Resumen financiero
                    col.Item().Text("RESUMEN FINANCIERO").Bold().FontSize(11);
                    col.Item().PaddingBottom(6).Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.ConstantColumn(80);
                        });
                        t.Cell().Element(CellStyle).Text("Total Facturado"); t.Cell().Element(CellStyle).AlignRight().Text(data.Resumen.TotalFacturado.ToString("N2", Ci));
                        t.Cell().Element(CellStyle).Text("Total Cobrado"); t.Cell().Element(CellStyle).AlignRight().Text(data.Resumen.TotalCobrado.ToString("N2", Ci));
                        t.Cell().Element(CellStyle).Text("Total Pendiente"); t.Cell().Element(CellStyle).AlignRight().Text(data.Resumen.TotalPendiente.ToString("N2", Ci));
                        t.Cell().Element(CellStyle).Text("Total Anulado"); t.Cell().Element(CellStyle).AlignRight().Text(data.Resumen.TotalAnulado.ToString("N2", Ci));
                        t.Cell().Element(CellStyle).Text("Descuentos"); t.Cell().Element(CellStyle).AlignRight().Text(data.Resumen.DescuentosAplicados.ToString("N2", Ci));
                        t.Cell().Element(CellStyle).Text("Total Neto Real").Bold(); t.Cell().Element(CellStyle).AlignRight().Text(data.Resumen.TotalNetoReal.ToString("N2", Ci)).Bold();
                    });

                    col.Item().PaddingTop(12).Text("DETALLE DE INGRESOS (resumen)").Bold().FontSize(11);
                    col.Item().PaddingBottom(4).Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(22); // Fecha
                            c.RelativeColumn();   // Paciente
                            c.RelativeColumn();   // Doctor
                            c.ConstantColumn(55); // Total
                            c.ConstantColumn(55); // Pagado
                            c.ConstantColumn(55); // Saldo
                            c.ConstantColumn(50); // Estado
                        });
                        t.Header(h =>
                        {
                            h.Cell().Element(HeaderStyle).Text("Fecha");
                            h.Cell().Element(HeaderStyle).Text("Paciente");
                            h.Cell().Element(HeaderStyle).Text("Doctor");
                            h.Cell().Element(HeaderStyle).AlignRight().Text("Total");
                            h.Cell().Element(HeaderStyle).AlignRight().Text("Pagado");
                            h.Cell().Element(HeaderStyle).AlignRight().Text("Saldo");
                            h.Cell().Element(HeaderStyle).Text("Estado");
                        });
                        foreach (var r in data.DetalleIngresos.Take(50))
                        {
                            t.Cell().Element(CellStyle).Text(r.Fecha.ToString("dd/MM/yyyy", Ci));
                            t.Cell().Element(CellStyle).Text(r.Paciente.Length > 25 ? r.Paciente[..25] + "..." : r.Paciente);
                            t.Cell().Element(CellStyle).Text(r.Doctor.Length > 18 ? r.Doctor[..18] + "..." : r.Doctor);
                            t.Cell().Element(CellStyle).AlignRight().Text(r.MontoTotal.ToString("N2", Ci));
                            t.Cell().Element(CellStyle).AlignRight().Text(r.MontoPagado.ToString("N2", Ci));
                            t.Cell().Element(CellStyle).AlignRight().Text(r.SaldoPendiente.ToString("N2", Ci));
                            t.Cell().Element(CellStyle).Text(r.Estado);
                        }
                    });

                    if (data.DetalleIngresos.Count > 50)
                        col.Item().Text("... y " + (data.DetalleIngresos.Count - 50) + " registros más.").FontSize(8).Italic();

                    col.Item().PaddingTop(10).Text("CUENTAS POR COBRAR").Bold().FontSize(11);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.ConstantColumn(70);
                            c.ConstantColumn(75);
                            c.ConstantColumn(50);
                        });
                        t.Header(h =>
                        {
                            h.Cell().Element(HeaderStyle).Text("Paciente");
                            h.Cell().Element(HeaderStyle).AlignRight().Text("Total pendiente");
                            h.Cell().Element(HeaderStyle).Text("Últ. atención");
                            h.Cell().Element(HeaderStyle).AlignRight().Text("Días venc.");
                        });
                        foreach (var r in data.CuentasPorCobrar.Take(25))
                        {
                            var pac = r.Paciente.Length > 30 ? r.Paciente[..30] + "..." : r.Paciente;
                            var ult = r.UltimaFechaAtencion.HasValue ? r.UltimaFechaAtencion.Value.ToString("dd/MM/yyyy", Ci) : "-";
                            var dias = r.DiasVencidos.HasValue ? r.DiasVencidos.Value.ToString() : "-";
                            t.Cell().Element(CellStyle).Text(pac);
                            t.Cell().Element(CellStyle).AlignRight().Text(r.TotalPendiente.ToString("N2", Ci));
                            t.Cell().Element(CellStyle).Text(ult);
                            t.Cell().Element(CellStyle).AlignRight().Text(dias);
                        }
                    });

                    col.Item().PaddingTop(8).Text("Observaciones: Reporte generado desde Odontari. Para detalle completo use la exportación Excel.").FontSize(8).Italic();
                });
            });
        });

        return doc.GeneratePdf();
    }

    private static IContainer CellStyle(IContainer c) => c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4);
    private static IContainer HeaderStyle(IContainer c) => c.DefaultTextStyle(x => x.Bold()).BorderBottom(1).BorderColor(Colors.Grey.Medium).PaddingVertical(4).PaddingHorizontal(4);
}
