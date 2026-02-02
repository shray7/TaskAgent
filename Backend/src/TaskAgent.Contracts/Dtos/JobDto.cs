namespace TaskAgent.Contracts.Dtos;

/// <summary>
/// Job data transfer object matching the generate-jobs.cjs schema.
/// </summary>
public record JobDto(
    string Id,
    string Title,
    string Description,
    string Company,
    string Location,
    string Address,
    DateTime ScheduleTime,
    DateTime ScheduleDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
