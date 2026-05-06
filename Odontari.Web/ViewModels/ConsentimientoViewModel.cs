using System.ComponentModel.DataAnnotations;
using Odontari.Web.Models.Enums;

namespace Odontari.Web.ViewModels;

public class PlantillaConsentimientoListViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string Titulo { get; set; } = null!;
    public bool EsObligatorio { get; set; }
    public bool Activo { get; set; }
    public int Version { get; set; }
    public DateTime FechaCreacion { get; set; }
    public int TotalDocumentos { get; set; }
}

public class PlantillaConsentimientoEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(200)]
    public string Nombre { get; set; } = null!;

    [Required(ErrorMessage = "El título es obligatorio")]
    [MaxLength(300)]
    public string Titulo { get; set; } = null!;

    [Required(ErrorMessage = "El texto base es obligatorio")]
    public string TextoBase { get; set; } = null!;

    public bool EsObligatorio { get; set; }
    public bool Activo { get; set; } = true;
    public int Version { get; set; } = 1;
}

public class ConsentimientoGeneradoListViewModel
{
    public int Id { get; set; }
    public string PlantillaNombre { get; set; } = null!;
    public string PacienteNombre { get; set; } = null!;
    public string DoctorNombre { get; set; } = null!;
    public string? NombreFirmante { get; set; }
    public EstadoConsentimiento Estado { get; set; }
    public DateTime FechaGeneracion { get; set; }
    public DateTime? FechaFirma { get; set; }
    public int CitaId { get; set; }
}

public class FirmarConsentimientoViewModel
{
    public int ConsentimientoId { get; set; }
    public int CitaId { get; set; }

    // Datos de contexto (read-only, para mostrar en la página de firma)
    public string ClinicaNombre { get; set; } = null!;
    public string? ClinicaLogo { get; set; }
    public string TituloDocumento { get; set; } = null!;
    public string TextoFinal { get; set; } = null!;
    public string PacienteNombre { get; set; } = null!;
    public string DoctorNombre { get; set; } = null!;
    public string FechaGeneracion { get; set; } = null!;
    public EstadoConsentimiento Estado { get; set; }
}

public class GenerarConsentimientoViewModel
{
    public int CitaId { get; set; }
    public int PlantillaId { get; set; }
    public int? ProcedimientoRealizadoId { get; set; }
}
