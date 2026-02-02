namespace TaskAgent.DataAccess.Entities;

/// <summary>
/// Project entity for task management (Vue app compatibility).
/// </summary>
public class ProjectEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int OwnerId { get; set; }
    public string? VisibleToUserIdsJson { get; set; } // JSON array of int, e.g. [1,2]
    public int SprintDurationDays { get; set; } = 14;
    public string? VisibleColumnsJson { get; set; } // JSON array: ["todo","in-progress","completed"]
    public string TaskSizeUnit { get; set; } = "hours"; // "hours" | "days"
    
    /// <summary>
    /// Soft-delete timestamp. Null means the project is active.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    public AppUser Owner { get; set; } = null!;
}
