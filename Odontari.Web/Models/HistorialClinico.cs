namespace Odontari.Web.Models;

/// <summary>
/// Evento en la línea de tiempo clínica del paciente (histograma).
/// Se crea automáticamente el primero al registrar al paciente.
/// </summary>
public class HistorialClinico
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;
    public int ClinicaId { get; set; }
    public Clinica Clinica { get; set; } = null!;

    public DateTime FechaEvento { get; set; }
    public string TipoEvento { get; set; } = null!; // Creación de expediente, Tratamiento, Diagnóstico, etc.
    public string? Descripcion { get; set; }

    public string? UsuarioId { get; set; }
    public int? CitaId { get; set; }
    public Cita? Cita { get; set; }
}
