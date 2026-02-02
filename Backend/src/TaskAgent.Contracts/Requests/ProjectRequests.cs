namespace TaskAgent.Contracts.Requests;

public record CreateProjectRequest(
    string Name,
    string? Description,
    string? Color,
    int OwnerId,
    DateTime? StartDate,
    DateTime? EndDate,
    int[]? VisibleToUserIds,
    int? SprintDurationDays,
    string[]? VisibleColumns,
    string? TaskSizeUnit);

public record UpdateProjectRequest(
    string? Name,
    string? Description,
    string? Color,
    int? OwnerId,
    DateTime? StartDate,
    DateTime? EndDate,
    int[]? VisibleToUserIds,
    int? SprintDurationDays,
    string[]? VisibleColumns,
    string? TaskSizeUnit);
