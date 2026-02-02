namespace TaskAgent.DataAccess.Entities;

/// <summary>
/// Task entity for task management (Vue app compatibility).
/// Named TaskItem to avoid conflict with System.Threading.Tasks.Task.
/// </summary>
public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "todo"; // "todo" | "in-progress" | "completed"
    public string Priority { get; set; } = "medium"; // "low" | "medium" | "high"
    public int AssigneeId { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public string? TagsJson { get; set; } // JSON array of strings
    public int ProjectId { get; set; }
    public int? SprintId { get; set; }
    public decimal? Size { get; set; } // Estimated size in hours or days
    
    /// <summary>
    /// Soft-delete timestamp. Null means the task is active.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    public ProjectEntity Project { get; set; } = null!;
    public SprintEntity? Sprint { get; set; }
}
