namespace Odontari.Web.Models;

/// <summary>Tipo de odontograma: adulto (32 dientes FDI permanente) o infantil (20 dientes FDI temporal).</summary>
public enum TipoOdontograma
{
    Adulto = 0,
    Infantil = 1
}

/// <summary>
/// Estado dental del paciente. El campo EstadoJSON almacena la estructura
/// según odontogramapro.mdc: teeth (32 adultos o 20 infantiles), superficies y estados.
/// </summary>
public class Odontograma
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;
    public int ClinicaId { get; set; }
    public Clinica Clinica { get; set; } = null!;

    /// <summary>0 = Adulto (32 dientes), 1 = Infantil (20 dientes temporales).</summary>
    public TipoOdontograma TipoOdontograma { get; set; } = TipoOdontograma.Adulto;

    /// <summary>JSON con estructura teeth, superficies, estados (odontogramapro.mdc).</summary>
    public string EstadoJson { get; set; } = "{}";
    public string? Observaciones { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public DateTime UltimaModificacion { get; set; } = DateTime.UtcNow;
    public string? UltimoUsuarioId { get; set; }
}
