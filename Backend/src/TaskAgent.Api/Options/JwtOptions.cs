namespace TaskAgent.Api.Options;

/// <summary>
/// Configuration for JWT bearer token generation and validation.
/// Bind from "Jwt" section in appsettings.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Issuer claim (e.g. "TaskAgent.Api").</summary>
    public string Issuer { get; set; } = "TaskAgent.Api";

    /// <summary>Audience claim (e.g. "TaskAgent.App").</summary>
    public string Audience { get; set; } = "TaskAgent.App";

    /// <summary>Base64-encoded or plain secret key. Must be at least 32 characters for HMAC-SHA256.</summary>
    public string Key { get; set; } = "";

    /// <summary>Token lifetime in minutes.</summary>
    public int ExpirationMinutes { get; set; } = 60 * 24; // 24 hours
}
