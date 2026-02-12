using Odontari.Web.Models.Enums;

namespace Odontari.Web.Models;

public class OrdenCobro
{
    public int Id { get; set; }
    public int ClinicaId { get; set; }
    public Clinica Clinica { get; set; } = null!;
    public int PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;
    public int? CitaId { get; set; }
    public Cita? Cita { get; set; }

    public decimal Total { get; set; }
    public decimal MontoPagado { get; set; }
    public EstadoCobro Estado { get; set; } = EstadoCobro.Pendiente;
    public DateTime CreadoAt { get; set; }

    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}
