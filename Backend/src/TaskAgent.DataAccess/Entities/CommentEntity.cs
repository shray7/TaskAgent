namespace TaskAgent.DataAccess.Entities;

/// <summary>
/// Comment entity - can be attached to a project or a task.
/// Exactly one of ProjectId or TaskId must be set.
/// </summary>
public class CommentEntity
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int AuthorId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? ProjectId { get; set; }
    public int? TaskId { get; set; }

    public AppUser Author { get; set; } = null!;
}
