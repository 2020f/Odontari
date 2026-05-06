using Odontari.Web.Models.Enums;

namespace Odontari.Web.Models;

/// <summary>
/// Documento de consentimiento informado generado para un paciente específico,
/// vinculado a una cita. Contiene el texto final (snapshot de la plantilla en el
/// momento de generación) y la firma digital capturada del paciente.
/// </summary>
public class ConsentimientoGenerado
{
    public int Id { get; set; }

    /// <summary>Clínica propietaria — filtro multi-tenant obligatorio en todas las queries.</summary>
    public int ClinicaId { get; set; }
    public Clinica Clinica { get; set; } = null!;

    public int PlantillaId { get; set; }
    public PlantillaConsentimiento Plantilla { get; set; } = null!;

    public int PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;

    public int CitaId { get; set; }
    public Cita Cita { get; set; } = null!;

    /// <summary>Procedimiento específico al que aplica. Null = aplica a toda la cita.</summary>
    public int? ProcedimientoRealizadoId { get; set; }
    public ProcedimientoRealizado? ProcedimientoRealizado { get; set; }

    /// <summary>Doctor que generó y confirma el documento.</summary>
    public string DoctorId { get; set; } = null!;
    public ApplicationUser? Doctor { get; set; }

    /// <summary>
    /// Texto final del documento con los datos del paciente ya interpolados.
    /// Se guarda como evidencia permanente: si la plantilla cambia luego, este registro
    /// conserva exactamente lo que el paciente firmó.
    /// </summary>
    public string TextoFinal { get; set; } = null!;

    /// <summary>Versión de la plantilla utilizada al generar este documento.</summary>
    public int VersionPlantilla { get; set; }

    public EstadoConsentimiento Estado { get; set; } = EstadoConsentimiento.Pendiente;

    public DateTime FechaGeneracion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaFirma { get; set; }

    /// <summary>Firma digital capturada del canvas HTML5, almacenada en base64 (PNG).</summary>
    public string? FirmaDigitalBase64 { get; set; }

    /// <summary>Nombre de quien firma (paciente u otra persona autorizada).</summary>
    public string? NombreFirmante { get; set; }

    public string? Observacion { get; set; }
}
