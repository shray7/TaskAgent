using TaskAgent.Contracts.Dtos;

namespace TaskAgent.Api.Services;

public interface IUsersService
{
    Task<IEnumerable<AppUserDto>> GetAllAsync(CancellationToken ct = default);
    Task<AppUserDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<AppUserDto?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}
