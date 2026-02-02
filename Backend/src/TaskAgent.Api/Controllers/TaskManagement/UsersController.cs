using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskAgent.Api.Services;
using TaskAgent.Contracts.Dtos;
using TaskAgent.DataAccess.Entities;
using TaskAgent.DataAccess.Sql;

namespace TaskAgent.Api.Controllers.TaskManagement;

[ApiController]
[Route("api/tm/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public UsersController(AppDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    /// <summary>Get all users (for assignee dropdown, etc.)</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppUserDto>>> GetAll(CancellationToken ct)
    {
        var users = await _db.AppUsers.ToListAsync(ct);
        return Ok(users.Select(Map));
    }

    /// <summary>Get user by ID</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppUserDto>> GetById(int id, CancellationToken ct)
    {
        var user = await _db.AppUsers.FindAsync([id], ct);
        if (user == null) return NotFound();
        return Ok(Map(user));
    }

    /// <summary>Get user by email (for checking if email exists)</summary>
    [HttpGet("by-email/{email}")]
    public async Task<ActionResult<AppUserDto>> GetByEmail(string email, CancellationToken ct)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user == null) return NotFound();
        return Ok(Map(user));
    }

    /// <summary>Register a new user with email and password</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        // Check if email already exists
        var existingUser = await _db.AppUsers.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        if (existingUser != null)
        {
            return BadRequest(new AuthResponse(false, "Email already registered", null));
        }

        // Generate avatar emoji based on name
        var avatars = new[] { "👩‍💼", "👨‍💻", "👩‍🎨", "👨‍🔬", "👩‍🏫", "👨‍🎤", "👩‍⚕️", "👨‍🍳" };
        var avatar = avatars[Math.Abs(request.Name.GetHashCode()) % avatars.Length];

        // Create new user with hashed password
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

        return Ok(new AuthResponse(true, "Registration successful", Map(user)));
    }

    /// <summary>Login with email and password</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(
            u => u.Email == request.Email.ToLowerInvariant().Trim(), 
            ct
        );

        if (user == null)
        {
            return Unauthorized(new AuthResponse(false, "Invalid email or password", null));
        }

        // If user has no password (legacy/demo user), allow login with any password for backwards compatibility
        // In production, you'd want to require password reset
        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            return Ok(new AuthResponse(true, "Login successful", Map(user)));
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new AuthResponse(false, "Invalid email or password", null));
        }

        return Ok(new AuthResponse(true, "Login successful", Map(user)));
    }

    private static AppUserDto Map(AppUser u) => new(u.Id, u.Name, u.Email, u.Avatar);
}
