namespace TaskAgent.Api.Services;

/// <summary>
/// Generates JWT bearer tokens for authenticated users.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>Generate a JWT for the given user (e.g. after login/register).</summary>
    string GenerateToken(int userId, string email);
}
