using TaskAgent.Contracts.Dtos;
using TaskAgent.Contracts.Requests;

namespace TaskAgent.Api.Services;

public interface ISprintsService
{
    Task<IEnumerable<SprintDto>> GetAllAsync(int? projectId, CancellationToken ct = default);
    Task<SprintDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(SprintDto? Dto, string? Error)> CreateAsync(CreateSprintRequest req, CancellationToken ct = default);
    Task<(SprintDto? Dto, string? Error)> UpdateAsync(int id, UpdateSprintRequest req, CancellationToken ct = default);
    Task<(SprintDto? Dto, string? Error)> StartAsync(int id, CancellationToken ct = default);
    Task<(SprintDto? Dto, string? Error)> CompleteAsync(int id, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
