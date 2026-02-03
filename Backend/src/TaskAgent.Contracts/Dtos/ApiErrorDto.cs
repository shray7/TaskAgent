namespace TaskAgent.Contracts.Dtos;

/// <summary>
/// Standard error payload for API 4xx/5xx responses so clients can read a consistent "message" field.
/// </summary>
public record ApiErrorDto(string Message, string? Code = null);
