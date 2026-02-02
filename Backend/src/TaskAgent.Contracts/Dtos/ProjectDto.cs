namespace TaskAgent.Contracts.Dtos;

public record ProjectDto(
    int Id,
    string Name,
    string Description,
    string Color,
    DateTime CreatedAt,
    DateTime? StartDate,
    DateTime? EndDate,
    int OwnerId,
    int[]? VisibleToUserIds,
    int SprintDurationDays,
    string[]? VisibleColumns,
    string TaskSizeUnit);
