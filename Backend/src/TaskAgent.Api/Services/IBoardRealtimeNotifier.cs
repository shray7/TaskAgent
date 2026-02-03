using TaskAgent.Contracts.Dtos;

namespace TaskAgent.Api.Services;

public interface IBoardRealtimeNotifier
{
    Task NotifyTaskCreatedAsync(TaskItemDto task, CancellationToken ct = default);
    Task NotifyTaskUpdatedAsync(TaskItemDto task, CancellationToken ct = default);
    Task NotifyTaskDeletedAsync(int projectId, int? sprintId, int taskId, CancellationToken ct = default);
}
