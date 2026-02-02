namespace TaskAgent.Contracts.Requests;

public record CreateSprintRequest(int ProjectId, string Name, string? Goal, DateTime StartDate);

public record UpdateSprintRequest(string? Name, string? Goal, DateTime? StartDate, DateTime? EndDate, string? Status);
