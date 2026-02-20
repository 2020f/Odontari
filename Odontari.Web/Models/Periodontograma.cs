namespace Odontari.Web.Models;

/// <summary>
/// Registro periodontal del paciente. EstadoJson sigue la especificación:
/// { "superior": { "18": {...}, ... "28": {...} }, "inferior": { "48": {...}, ... "38": {...} } }
/// </summary>
public class Periodontograma
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;
    public int ClinicaId { get; set; }
    public Clinica Clinica { get; set; } = null!;

    /// <summary>JSON con estructura superior/inferior y datos por diente (FDI 18-11|21-28, 48-41|31-38).</summary>
    public string EstadoJson { get; set; } = "{}";

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public DateTime UltimaModificacion { get; set; } = DateTime.UtcNow;
    public string? UltimoUsuarioId { get; set; }
}
