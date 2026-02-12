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
}

public class PagoRegistroViewModel
{
    public int OrdenCobroId { get; set; }
    public decimal SaldoPendiente { get; set; }
    public decimal Monto { get; set; }
    public string? MetodoPago { get; set; }
    public string? Referencia { get; set; }
}
