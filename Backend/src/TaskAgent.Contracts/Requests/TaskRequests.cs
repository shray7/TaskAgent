namespace TaskAgent.Contracts.Requests;

public record CreateTaskRequest(
    string Title,
    string? Description,
    string? Status,
    string? Priority,
    int AssigneeId,
    int CreatedBy,
    int ProjectId,
    int? SprintId,
    DateTime? DueDate,
    string[]? Tags,
    decimal? Size);

public record UpdateTaskRequest(
    string? Title,
    string? Description,
    string? Status,
    string? Priority,
    int? AssigneeId,
    DateTime? DueDate,
    int? SprintId,
    string[]? Tags,
    decimal? Size);
