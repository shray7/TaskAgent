using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskAgent.Contracts.Dtos;

/// <summary>
/// Request to register a new user.
/// </summary>
public record RegisterRequest(
    [Required]
    [EmailAddress]
    [StringLength(256)]
    string Email,
    
    [Required]
    [StringLength(128, MinimumLength = 2)]
    string Name,
    
    [Required]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    string Password
);

/// <summary>
/// Request to login with email and password.
/// </summary>
public record LoginRequest(
    [Required]
    [EmailAddress]
    string Email,
    
    [Required]
    string Password
);

/// <summary>
/// Response after successful authentication. When Success is true, Token is set for JWT bearer auth.
/// </summary>
public record AuthResponse(
    bool Success,
    string? Message,
    AppUserDto? User,
    [property: JsonPropertyName("token")] string? Token
);
