using Odontari.Web.Models.Enums;

namespace Odontari.Web.ViewModels;

public class OrdenCobroListViewModel
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public string PacienteNombre { get; set; } = null!;
    public decimal Total { get; set; }
    public decimal MontoPagado { get; set; }
    public decimal SaldoPendiente => Total - MontoPagado;
    public EstadoCobro Estado { get; set; }
    public DateTime CreadoAt { get; set; }
    public int? CitaId { get; set; }
    /// <summary>Id de la factura asociada (si existe) para enlace "Descargar factura".</summary>
    public int? FacturaId { get; set; }
}

public class PagoRegistroViewModel
{
    public int OrdenCobroId { get; set; }
    public decimal SaldoPendiente { get; set; }
    public decimal Monto { get; set; }
    public string? MetodoPago { get; set; }
    public string? Referencia { get; set; }
}

/// <summary>Una fila del historial de pagos (un pago con datos de orden y paciente).</summary>
public class HistorialPagoItemViewModel
{
    public int PagoId { get; set; }
    public DateTime FechaPago { get; set; }
    public string PacienteNombre { get; set; } = null!;
    public int OrdenCobroId { get; set; }
    public decimal OrdenTotal { get; set; }
    public decimal Monto { get; set; }
    public string? MetodoPago { get; set; }
    public string? Referencia { get; set; }
    public int? FacturaId { get; set; }
}
