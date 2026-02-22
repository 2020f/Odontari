using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Odontari.Web.Services;

public class BlobUploadService : IBlobUploadService
{
    private readonly string _connectionString;
    private readonly string _containerName;
    private const int MaxSizeBytes = 10 * 1024 * 1024; // 10 MB

    public BlobUploadService(IConfiguration configuration)
    {
        _connectionString = configuration["AzureBlob:ConnectionString"] ?? "";
        _containerName = configuration["AzureBlob:Container"] ?? "odontari-archivos";
    }

    public string ContainerName => _containerName;

    public async Task<string> UploadAsync(Stream content, string contentType, string extension, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("AzureBlob:ConnectionString no está configurado.");

        var client = new BlobServiceClient(_connectionString);
        var container = client.GetBlobContainerClient(_containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.None, metadata: null, encryptionScopeOptions: null, cancellationToken).ConfigureAwait(false);

        var blobName = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{NormalizeExtension(extension)}";
        var blob = container.GetBlobClient(blobName);

        if (content.CanSeek && content.Length > MaxSizeBytes)
            throw new InvalidOperationException($"El archivo no puede superar {MaxSizeBytes / (1024 * 1024)} MB.");

        await blob.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken).ConfigureAwait(false);
        return blobName;
    }

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString) || string.IsNullOrWhiteSpace(blobName)) return;

        var client = new BlobServiceClient(_connectionString);
        var container = client.GetBlobContainerClient(_containerName);
        var blob = container.GetBlobClient(blobName);
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<Stream?> GetStreamAsync(string blobName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString) || string.IsNullOrWhiteSpace(blobName)) return null;

        var client = new BlobServiceClient(_connectionString);
        var container = client.GetBlobContainerClient(_containerName);
        var blob = container.GetBlobClient(blobName);
        if (!await blob.ExistsAsync(cancellationToken).ConfigureAwait(false))
            return null;

        var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return response.Value.Content;
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension)) return ".bin";
        extension = extension.Trim().ToLowerInvariant();
        if (!extension.StartsWith('.')) extension = "." + extension;
        return extension;
    }
}
