using Microsoft.EntityFrameworkCore;
using TaskAgent.Contracts.Dtos;
using TaskAgent.DataAccess.Entities;
using TaskAgent.DataAccess.Sql;

namespace TaskAgent.Api.Services;

public class UsersService : IUsersService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public UsersService(AppDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<IEnumerable<AppUserDto>> GetAllAsync(CancellationToken ct = default)
    {
        var users = await _db.AppUsers.ToListAsync(ct);
        return users.Select(Map);
    }

    public async Task<AppUserDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var user = await _db.AppUsers.FindAsync([id], ct);
        return user == null ? null : Map(user);
    }

    public async Task<AppUserDto?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Email == email, ct);
        return user == null ? null : Map(user);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var existingUser = await _db.AppUsers.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        if (existingUser != null)
            return new AuthResponse(false, "Email already registered", null);

        var avatars = new[] { "👩‍💼", "👨‍💻", "👩‍🎨", "👨‍🔬", "👩‍🏫", "👨‍🎤", "👩‍⚕️", "👨‍🍳" };
        var avatar = avatars[Math.Abs(request.Name.GetHashCode()) % avatars.Length];

        var user = new AppUser
        {
            Name = request.Name.Trim(),
            Email = request.Email.ToLowerInvariant().Trim(),
            Avatar = avatar,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync(ct);

        return new AuthResponse(true, "Registration successful", Map(user));
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(
            u => u.Email == request.Email.ToLowerInvariant().Trim(),
            ct);

        if (user == null)
            return new AuthResponse(false, "Invalid email or password", null);

        if (string.IsNullOrEmpty(user.PasswordHash))
            return new AuthResponse(true, "Login successful", Map(user));

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return new AuthResponse(false, "Invalid email or password", null);

        return new AuthResponse(true, "Login successful", Map(user));
    }

    private static AppUserDto Map(AppUser u) => new(u.Id, u.Name, u.Email, u.Avatar);
}
