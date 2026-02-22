namespace Odontari.Web.Models;

public class Paciente
{
    public int Id { get; set; }
    public int ClinicaId { get; set; }
    public Clinica Clinica { get; set; } = null!;

    public string Nombre { get; set; } = null!;
    public string? Apellidos { get; set; }
    public string? Cedula { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string? Alergias { get; set; }
    public string? NotasClinicas { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
    public ICollection<OrdenCobro> OrdenesCobro { get; set; } = new List<OrdenCobro>();

    // Expediente clínico (Fase 5)
    public ICollection<Odontograma> Odontogramas { get; set; } = new List<Odontograma>();
    public ICollection<Periodontograma> Periodontogramas { get; set; } = new List<Periodontograma>();
    public ICollection<HistorialClinico> HistorialClinico { get; set; } = new List<HistorialClinico>();
    public HistoriaClinicaSistematica? HistoriaClinicaSistematica { get; set; }
    public ICollection<ArchivoSubido> ArchivosSubidos { get; set; } = new List<ArchivoSubido>();
}
