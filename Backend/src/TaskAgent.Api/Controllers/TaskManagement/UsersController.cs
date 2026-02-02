using Microsoft.AspNetCore.Mvc;
using TaskAgent.Api.Services;
using TaskAgent.Contracts.Dtos;

namespace TaskAgent.Api.Controllers.TaskManagement;

[ApiController]
[Route("api/tm/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUsersService _users;

    public UsersController(IUsersService users) => _users = users;

    /// <summary>Get all users (for assignee dropdown, etc.)</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppUserDto>>> GetAll(CancellationToken ct)
    {
        var users = await _users.GetAllAsync(ct);
        return Ok(users);
    }

    /// <summary>Get user by ID</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppUserDto>> GetById(int id, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct);
        if (user == null) return NotFound();
        return Ok(user);
    }

    /// <summary>Get user by email (for checking if email exists)</summary>
    [HttpGet("by-email/{email}")]
    public async Task<ActionResult<AppUserDto>> GetByEmail(string email, CancellationToken ct)
    {
        var user = await _users.GetByEmailAsync(email, ct);
        if (user == null) return NotFound();
        return Ok(user);
    }

    /// <summary>Register a new user with email and password</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var response = await _users.RegisterAsync(request, ct);
        if (!response.Success)
            return BadRequest(response);
        return Ok(response);
    }

    /// <summary>Login with email and password</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var response = await _users.LoginAsync(request, ct);
        if (!response.Success)
            return Unauthorized(response);
        return Ok(response);
    }
}
