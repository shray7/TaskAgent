using TaskAgent.Contracts.Dtos;
using TaskAgent.Contracts.Requests;

namespace TaskAgent.Api.Services;

public interface ITasksService
{
    Task<IEnumerable<TaskItemDto>> GetAllAsync(int currentUserId, int? projectId, int? sprintId, string? status, CancellationToken ct = default);
    Task<TaskItemDto?> GetByIdAsync(int id, int currentUserId, CancellationToken ct = default);
    Task<(TaskItemDto? Dto, string? Error)> CreateAsync(CreateTaskRequest req, int currentUserId, CancellationToken ct = default);
    Task<(TaskItemDto? Dto, string? Error)> UpdateAsync(int id, UpdateTaskRequest req, int currentUserId, CancellationToken ct = default);
    Task<(bool Success, string? Error)> DeleteAsync(int id, int currentUserId, CancellationToken ct = default);
}
