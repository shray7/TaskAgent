namespace TaskAgent.DataAccess.Entities;

/// <summary>
/// Sprint entity for task management (Vue app compatibility).
/// </summary>
public class SprintEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "planning"; // "planning" | "active" | "completed"
    
    /// <summary>
    /// Soft-delete timestamp. Null means the sprint is active.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    public ProjectEntity Project { get; set; } = null!;
}
