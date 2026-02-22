namespace Odontari.Web.Services;

/// <summary>
/// Servicio para subir, eliminar y obtener archivos desde Azure Blob Storage.
/// El container se toma de configuración (AzureBlob:Container).
/// </summary>
public interface IBlobUploadService
{
    /// <summary>Sube un archivo al container configurado. Retorna el nombre del blob (ruta única).</summary>
    Task<string> UploadAsync(Stream content, string contentType, string extension, CancellationToken cancellationToken = default);

    /// <summary>Elimina un blob por nombre. No lanza si no existe.</summary>
    Task DeleteAsync(string blobName, CancellationToken cancellationToken = default);

    /// <summary>Obtiene el stream del blob para ver o descargar.</summary>
    Task<Stream?> GetStreamAsync(string blobName, CancellationToken cancellationToken = default);

    /// <summary>Nombre del container configurado.</summary>
    string ContainerName { get; }
}
