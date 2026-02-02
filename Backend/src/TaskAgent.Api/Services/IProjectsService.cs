using TaskAgent.Contracts.Dtos;
using TaskAgent.Contracts.Requests;

namespace TaskAgent.Api.Services;

public interface IProjectsService
{
    Task<IEnumerable<ProjectDto>> GetAllAsync(CancellationToken ct = default);
    Task<ProjectDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(ProjectDto? Dto, string? Error)> CreateAsync(CreateProjectRequest req, CancellationToken ct = default);
    Task<(ProjectDto? Dto, string? Error)> UpdateAsync(int id, UpdateProjectRequest req, CancellationToken ct = default);
    Task<int> DeleteAsync(int id, int userId, CancellationToken ct = default);
}
