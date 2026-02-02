using System.Security.Cryptography;

namespace TaskAgent.Api.Services;

/// <summary>
/// Secure password hashing using PBKDF2 with salt.
/// Uses cryptographically secure random salt and industry-standard iterations.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hash a password with a random salt.
    /// </summary>
    string HashPassword(string password);
    
    /// <summary>
    /// Verify a password against a stored hash.
    /// </summary>
    bool VerifyPassword(string password, string storedHash);
}

public class PasswordHasher : IPasswordHasher
{
    // PBKDF2 parameters - following OWASP recommendations
    private const int SaltSize = 16; // 128 bits
    private const int HashSize = 32; // 256 bits
    private const int Iterations = 100_000; // OWASP recommends 600,000 for PBKDF2-SHA256, but 100k is reasonable for most apps
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    /// <summary>
    /// Hash a password using PBKDF2 with a cryptographically secure random salt.
    /// Format: {iterations}.{salt-base64}.{hash-base64}
    /// </summary>
    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        // Generate a cryptographically secure random salt
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        
        // Hash the password with the salt using PBKDF2
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            Algorithm,
            HashSize
        );

        // Return as a storable string: iterations.salt.hash
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    /// <summary>
    /// Verify a password against a stored hash using constant-time comparison.
    /// </summary>
    public bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
            return false;

        // Parse the stored hash
        var parts = storedHash.Split('.');
        if (parts.Length != 3)
            return false;

        if (!int.TryParse(parts[0], out int iterations))
            return false;

        byte[] salt;
        byte[] storedHashBytes;
        
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            storedHashBytes = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        // Hash the provided password with the same salt and iterations
        byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            Algorithm,
            storedHashBytes.Length
        );

        // Use constant-time comparison to prevent timing attacks
        return CryptographicOperations.FixedTimeEquals(computedHash, storedHashBytes);
    }
}
