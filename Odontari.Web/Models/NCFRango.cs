using Odontari.Web.Models.Enums;

namespace Odontari.Web.Models;

/// <summary>Rango de NCF autorizado por DGII para una clínica. Un rango activo por tipo por clínica (recomendado).</summary>
public class NCFRango
{
    public int Id { get; set; }
    public int ClinicaId { get; set; }
    public Clinica Clinica { get; set; } = null!;
    public int NCFTipoId { get; set; }
    public NCFTipo NCFTipo { get; set; } = null!;

    public string? SeriePrefijo { get; set; }
    public string Desde { get; set; } = null!;   // Inicio del rango autorizado
    public string Hasta { get; set; } = null!;  // Fin del rango
    public long Proximo { get; set; }            // Siguiente NCF a usar (control automático)

    public DateTime? FechaAutorizacion { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public EstadoNCFRango Estado { get; set; } = EstadoNCFRango.Activo;
    public string Fuente { get; set; } = "Manual";  // Manual / DGII (futuro)
    public string? Nota { get; set; }
    public DateTime CreadoAt { get; set; }
}
