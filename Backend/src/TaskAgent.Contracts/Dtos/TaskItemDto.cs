namespace TaskAgent.Contracts.Dtos;

public record TaskItemDto(
    int Id,
    string Title,
    string Description,
    string Status,
    string Priority,
    int AssigneeId,
    int CreatedBy,
    DateTime CreatedAt,
    DateTime? DueDate,
    string[] Tags,
    int ProjectId,
    int? SprintId,
    decimal? Size);
