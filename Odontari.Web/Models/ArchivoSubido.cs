namespace Odontari.Web.Models;

/// <summary>
/// Registro de archivo (imagen o PDF) subido a Azure Blob Storage.
/// Solo se guarda en BD la referencia (URL, container, blob name, etc.); el binario está en Blob.
/// Multitenant: siempre filtrar por ClinicaId. Asociado a PacienteId.
/// </summary>
public class ArchivoSubido
{
    public int Id { get; set; }
    public int ClinicaId { get; set; }
    public Clinica Clinica { get; set; } = null!;
    public int PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;

    public string Container { get; set; } = null!;
    public string BlobName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string FileNameOriginal { get; set; } = null!;
    public string Extension { get; set; } = null!;
    public string Estado { get; set; } = "Activo";
    public string? Url { get; set; }

    /// <summary>Usuario que subió el archivo (doctor/recepción).</summary>
    public string? UsuarioId { get; set; }
    public ApplicationUser? Usuario { get; set; }
}
