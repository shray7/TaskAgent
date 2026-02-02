using TaskAgent.Contracts.Dtos;
using TaskAgent.Contracts.Requests;

namespace TaskAgent.Api.Services;

public interface ITasksService
{
    Task<IEnumerable<TaskItemDto>> GetAllAsync(int? projectId, int? sprintId, string? status, CancellationToken ct = default);
    Task<TaskItemDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(TaskItemDto? Dto, string? Error)> CreateAsync(CreateTaskRequest req, CancellationToken ct = default);
    Task<(TaskItemDto? Dto, string? Error)> UpdateAsync(int id, UpdateTaskRequest req, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
