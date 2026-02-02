using Microsoft.AspNetCore.Mvc;
using TaskAgent.FileService.Services;

namespace TaskAgent.FileService.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly IFileStorageService _storage;
    private readonly ILogger<FilesController> _logger;

    private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".json", ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".mp4", ".webm", ".mov", ".avi" };
    private const long MaxFileSizeBytes = 100 * 1024 * 1024;

    public FilesController(IFileStorageService storage, ILogger<FilesController> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSizeBytes)]
    public async Task<ActionResult<object>> Upload(IFormFile file, [FromQuery] string? scope, CancellationToken ct)
    {
        if (file == null || file.Length == 0) return BadRequest("No file provided");
        if (file.Length > MaxFileSizeBytes) return BadRequest($"File size exceeds {MaxFileSizeBytes / (1024 * 1024)} MB limit");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            return BadRequest($"File type not allowed. Allowed: {string.Join(", ", AllowedExtensions)}");

        try
        {
            await using var stream = file.OpenReadStream();
            var contentType = string.IsNullOrEmpty(file.ContentType) ? MimeTypes.GetMimeType(ext) : file.ContentType;
            var storageKey = await _storage.UploadAsync(stream, file.FileName, contentType, scope, ct);
            return Ok(new { storageKey, fileName = file.FileName, size = file.Length });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File upload failed");
            return StatusCode(500, "Upload failed");
        }
    }

    [HttpGet("{*storageKey}")]
    public async Task<IActionResult> Get(string storageKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(storageKey)) return BadRequest("Storage key required");
        var result = await _storage.GetAsync(storageKey, ct);
        if (result == null) return NotFound();
        return File(result.Stream, result.ContentType, result.FileName);
    }

    [HttpDelete("{*storageKey}")]
    public async Task<IActionResult> Delete(string storageKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(storageKey)) return BadRequest("Storage key required");
        var deleted = await _storage.DeleteAsync(storageKey, ct);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<object>>> List([FromQuery] string? scope, CancellationToken ct)
    {
        var entries = await _storage.ListAsync(scope, ct);
        var response = entries.Select(e => new { e.StorageKey, e.FileName, e.ContentType, e.Size, e.UploadedAt }).ToList();
        return Ok(response);
    }
}
