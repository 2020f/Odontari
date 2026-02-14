namespace Odontari.Web.ViewModels;

public class ExpedienteIndexViewModel
{
    public int PacienteId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Apellidos { get; set; }
    public string? Cedula { get; set; }
    public string? Telefono { get; set; }
    public string? Alergias { get; set; }
    public string? NotasClinicas { get; set; }
    public List<HistorialEventoViewModel> Historial { get; set; } = new();
}

public class HistorialEventoViewModel
{
    public int Id { get; set; }
    public int? CitaId { get; set; }
    public DateTime FechaEvento { get; set; }
    public string TipoEvento { get; set; } = null!;
    public string? Descripcion { get; set; }
}

/// <summary>Request para guardar odontograma vía API.</summary>
public class GuardarOdontogramaRequest
{
    public int PacienteId { get; set; }
    public string? EstadoJson { get; set; }
    /// <summary>Si está en contexto de cita (Atención/Expediente), se sincronizan hallazgos a procedimientos para cobro.</summary>
    public int? CitaId { get; set; }
}
