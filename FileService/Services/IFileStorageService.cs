namespace TaskAgent.FileService.Services;

public interface IFileStorageService
{
    Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        string? scope = null,
        CancellationToken cancellationToken = default);

    Task<FileStorageResult?> GetAsync(string storageKey, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FileStorageEntry>> ListAsync(string? scopePrefix = null, CancellationToken cancellationToken = default);
}

public record FileStorageResult(Stream Stream, string ContentType, string FileName);
public record FileStorageEntry(string StorageKey, string FileName, string ContentType, long Size, DateTimeOffset UploadedAt);
