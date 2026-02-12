namespace Odontari.Web.Models;

/// <summary>
/// Estado dental del paciente. El campo EstadoJSON almacena la estructura
/// según odontogramapro.mdc: teeth con 32 dientes, superficies y estados.
/// </summary>
public class Odontograma
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;
    public int ClinicaId { get; set; }
    public Clinica Clinica { get; set; } = null!;

    /// <summary>JSON con estructura teeth, superficies, estados (odontogramapro.mdc).</summary>
    public string EstadoJson { get; set; } = "{}";
    public string? Observaciones { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public DateTime UltimaModificacion { get; set; } = DateTime.UtcNow;
    public string? UltimoUsuarioId { get; set; }
}
