using Microsoft.EntityFrameworkCore;
using TaskAgent.Contracts.Dtos;
using TaskAgent.DataAccess.Entities;
using TaskAgent.DataAccess.Sql;

namespace TaskAgent.Api.Services;

public class UsersService : IUsersService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwt;

    public UsersService(AppDbContext db, IPasswordHasher passwordHasher, IJwtTokenService jwt)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
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
            return new AuthResponse(false, "Email already registered", null, null);

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

        var dto = Map(user);
        var token = _jwt.GenerateToken(user.Id, user.Email);
        return new AuthResponse(true, "Registration successful", dto, token);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(
            u => u.Email == request.Email.ToLowerInvariant().Trim(),
            ct);

        if (user == null)
            return new AuthResponse(false, "Invalid email or password", null, null);

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            var dto = Map(user);
            return new AuthResponse(true, "Login successful", dto, _jwt.GenerateToken(user.Id, user.Email));
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return new AuthResponse(false, "Invalid email or password", null, null);

        var mapped = Map(user);
        return new AuthResponse(true, "Login successful", mapped, _jwt.GenerateToken(user.Id, user.Email));
    }

    private static AppUserDto Map(AppUser u) => new(u.Id, u.Name, u.Email, u.Avatar);
}
