namespace TaskAgent.Contracts.Dtos;

public record SprintDto(
    int Id,
    int ProjectId,
    string Name,
    string Goal,
    DateTime StartDate,
    DateTime EndDate,
    string Status);
