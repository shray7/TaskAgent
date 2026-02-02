namespace TaskAgent.DataAccess.Entities;

/// <summary>
/// Job entity for database storage.
/// Matches the schema from generate-jobs.cjs script.
/// </summary>
public class Job
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime ScheduleTime { get; set; }
    public DateTime ScheduleDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
