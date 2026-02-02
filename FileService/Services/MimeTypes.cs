namespace TaskAgent.FileService.Services;

internal static class MimeTypes
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".json"] = "application/json",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".svg"] = "image/svg+xml",
        [".mp4"] = "video/mp4",
        [".webm"] = "video/webm",
        [".mov"] = "video/quicktime",
        [".avi"] = "video/x-msvideo",
    };

    public static string GetMimeType(string extension)
    {
        if (string.IsNullOrEmpty(extension)) return "application/octet-stream";
        if (!extension.StartsWith('.')) extension = "." + extension;
        return Map.TryGetValue(extension, out var mime) ? mime : "application/octet-stream";
    }
}
