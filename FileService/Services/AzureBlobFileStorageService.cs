using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace TaskAgent.FileService.Services;

public sealed class AzureBlobFileStorageService : IFileStorageService
{
    private readonly BlobContainerClient _container;
    private readonly ILogger<AzureBlobFileStorageService> _logger;

    public AzureBlobFileStorageService(IConfiguration configuration, ILogger<AzureBlobFileStorageService> logger)
    {
        var connectionString = configuration["FileStorage:AzureConnectionString"] ?? configuration["ConnectionStrings:AzureStorage"];
        var containerName = configuration["FileStorage:AzureContainerName"] ?? "taskflow-files";

        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("FileStorage:AzureConnectionString or ConnectionStrings:AzureStorage must be set when using Azure Blob storage.");

        var client = new BlobServiceClient(connectionString);
        _container = client.GetBlobContainerClient(containerName);
        _logger = logger;
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, string? scope = null, CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext)) ext = ".bin";
        var uniqueId = Guid.NewGuid().ToString("N")[..12];
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrEmpty(baseName)) baseName = "file";
        var safeBase = baseName.Length > 50 ? baseName[..50] : baseName;
        var blobName = string.IsNullOrEmpty(scope) ? $"{safeBase}-{uniqueId}{ext}" : $"{scope.TrimEnd('/')}/{safeBase}-{uniqueId}{ext}";

        await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
        var blob = _container.GetBlobClient(blobName);
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken);

        _logger.LogInformation("Uploaded file to Azure Blob: {Key}", blobName);
        return blobName;
    }

    public async Task<FileStorageResult?> GetAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(storageKey);
        if (!await blob.ExistsAsync(cancellationToken)) return null;

        var props = await blob.GetPropertiesAsync(cancellationToken: cancellationToken);
        var download = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        var contentType = props.Value.ContentType ?? MimeTypes.GetMimeType(Path.GetExtension(storageKey));
        return new FileStorageResult(download.Value.Content, contentType, Path.GetFileName(storageKey));
    }

    public async Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(storageKey);
        var deleted = await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        if (deleted.Value) _logger.LogInformation("Deleted Azure Blob: {Key}", storageKey);
        return deleted.Value;
    }

    public async Task<IReadOnlyList<FileStorageEntry>> ListAsync(string? scopePrefix = null, CancellationToken cancellationToken = default)
    {
        var entries = new List<FileStorageEntry>();
        var prefix = scopePrefix?.TrimEnd('/') ?? "";
        await foreach (var item in _container.GetBlobsAsync(prefix: string.IsNullOrEmpty(prefix) ? null : prefix + "/", cancellationToken: cancellationToken))
        {
            entries.Add(new FileStorageEntry(
                item.Name,
                Path.GetFileName(item.Name),
                item.Properties.ContentType ?? MimeTypes.GetMimeType(Path.GetExtension(item.Name)),
                item.Properties.ContentLength ?? 0,
                item.Properties.CreatedOn ?? DateTimeOffset.UtcNow));
        }
        return entries;
    }
}
