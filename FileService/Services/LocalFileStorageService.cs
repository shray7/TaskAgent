using System.Security.Cryptography;

namespace TaskAgent.FileService.Services;

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
    {
        _basePath = configuration["FileStorage:LocalPath"] ?? Path.Combine(Path.GetTempPath(), "taskflow-uploads");
        _logger = logger;
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, string? scope = null, CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext)) ext = ".bin";
        var uniqueId = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrEmpty(baseName)) baseName = "file";
        var safeBase = baseName.Length > 50 ? baseName[..50] : baseName;
        var safeName = $"{safeBase}-{uniqueId}{ext}";
        var scopeDir = string.IsNullOrEmpty(scope) ? "" : scope.Replace("/", Path.DirectorySeparatorChar.ToString());
        var fullDir = string.IsNullOrEmpty(scopeDir) ? _basePath : Path.Combine(_basePath, scopeDir);
        Directory.CreateDirectory(fullDir);
        var fullPath = Path.Combine(fullDir, safeName);
        var storageKey = string.IsNullOrEmpty(scope) ? safeName : $"{scope}/{safeName}";

        await using var fs = File.Create(fullPath);
        await stream.CopyToAsync(fs, cancellationToken);

        _logger.LogInformation("Uploaded file locally: {Key}", storageKey);
        return storageKey;
    }

    public Task<FileStorageResult?> GetAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storageKey);
        if (!File.Exists(fullPath)) return Task.FromResult<FileStorageResult?>(null);

        var contentType = MimeTypes.GetMimeType(Path.GetExtension(storageKey));
        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<FileStorageResult?>(new FileStorageResult(stream, contentType, Path.GetFileName(storageKey)));
    }

    public Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storageKey);
        if (!File.Exists(fullPath)) return Task.FromResult(false);
        File.Delete(fullPath);
        _logger.LogInformation("Deleted local file: {Key}", storageKey);
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<FileStorageEntry>> ListAsync(string? scopePrefix = null, CancellationToken cancellationToken = default)
    {
        var dir = string.IsNullOrEmpty(scopePrefix) ? _basePath : GetFullPath(scopePrefix);
        if (!Directory.Exists(dir)) return Task.FromResult<IReadOnlyList<FileStorageEntry>>(Array.Empty<FileStorageEntry>());

        var entries = new List<FileStorageEntry>();
        foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(_basePath, f);
            var key = rel.Replace(Path.DirectorySeparatorChar, '/');
            if (!string.IsNullOrEmpty(scopePrefix) && !key.StartsWith(scopePrefix, StringComparison.OrdinalIgnoreCase)) continue;

            var fi = new FileInfo(f);
            entries.Add(new FileStorageEntry(key, Path.GetFileName(f), MimeTypes.GetMimeType(Path.GetExtension(f)), fi.Length, fi.CreationTimeUtc));
        }
        return Task.FromResult<IReadOnlyList<FileStorageEntry>>(entries);
    }

    private string GetFullPath(string storageKey)
    {
        return Path.Combine(_basePath, storageKey.Replace('/', Path.DirectorySeparatorChar));
    }
}
