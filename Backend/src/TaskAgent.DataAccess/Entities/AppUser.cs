namespace TaskAgent.DataAccess.Entities;

/// <summary>
/// User entity for task management (Vue app compatibility).
/// </summary>
public class AppUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    
    /// <summary>
    /// Hashed password using PBKDF2 with salt.
    /// Format: {iterations}.{salt-base64}.{hash-base64}
    /// </summary>
    public string? PasswordHash { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
