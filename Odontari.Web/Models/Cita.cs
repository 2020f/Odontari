using Odontari.Web.Models.Enums;

namespace Odontari.Web.Models;

public class Cita
{
    public int Id { get; set; }
    public int ClinicaId { get; set; }
    public Clinica Clinica { get; set; } = null!;
    public int PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;
    /// <summary>UserId del doctor (ApplicationUser con rol Doctor).</summary>
    public string DoctorId { get; set; } = null!;
    public ApplicationUser? Doctor { get; set; }

    public DateTime FechaHora { get; set; }
    public string? Motivo { get; set; }
    public EstadoCita Estado { get; set; } = EstadoCita.Solicitada;
    public DateTime? InicioAtencionAt { get; set; }
    public DateTime? FinAtencionAt { get; set; }

    public ICollection<ProcedimientoRealizado> ProcedimientosRealizados { get; set; } = new List<ProcedimientoRealizado>();
    public ICollection<OrdenCobro> OrdenesCobro { get; set; } = new List<OrdenCobro>();
}
