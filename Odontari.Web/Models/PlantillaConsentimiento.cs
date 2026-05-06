namespace Odontari.Web.Models;

/// <summary>
/// Plantilla de consentimiento informado configurada por el administrador de la clínica.
/// El doctor la invoca desde la atención y el sistema autocompleta los datos del paciente.
/// </summary>
public class PlantillaConsentimiento
{
    public int Id { get; set; }

    /// <summary>Clínica propietaria — filtro multi-tenant obligatorio en todas las queries.</summary>
    public int ClinicaId { get; set; }
    public Clinica Clinica { get; set; } = null!;

    /// <summary>Nombre interno para identificar la plantilla en el backoffice.</summary>
    public string Nombre { get; set; } = null!;

    /// <summary>Título visible en el documento que se muestra al paciente.</summary>
    public string Titulo { get; set; } = null!;

    /// <summary>
    /// Texto base del consentimiento. Soporta marcadores que se reemplazan al generar:
    /// {NOMBRE_PACIENTE}, {CEDULA}, {EDAD}, {DOCTOR}, {FECHA}, {HORA}, {CLINICA}.
    /// </summary>
    public string TextoBase { get; set; } = null!;

    /// <summary>Si true, el procedimiento no puede marcarse finalizado sin consentimiento firmado.</summary>
    public bool EsObligatorio { get; set; }

    public bool Activo { get; set; } = true;

    /// <summary>Incrementa automáticamente cuando se edita el TextoBase. Los documentos firmados guardan la versión usada.</summary>
    public int Version { get; set; } = 1;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }

    public ICollection<ConsentimientoGenerado> Consentimientos { get; set; } = new List<ConsentimientoGenerado>();
}
