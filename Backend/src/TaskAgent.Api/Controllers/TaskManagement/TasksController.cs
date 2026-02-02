using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskAgent.Contracts.Dtos;
using TaskAgent.DataAccess.Entities;
using TaskAgent.DataAccess.Sql;

namespace TaskAgent.Api.Controllers.TaskManagement;

[ApiController]
[Route("api/tm/[controller]")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _db;

    public TasksController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskItemDto>>> GetAll(
        [FromQuery] int? projectId,
        [FromQuery] int? sprintId,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var query = _db.TaskItems.Where(t => t.DeletedAt == null);
        if (projectId.HasValue) query = query.Where(t => t.ProjectId == projectId.Value);
        if (sprintId.HasValue) query = query.Where(t => t.SprintId == sprintId.Value);
        if (!string.IsNullOrEmpty(status)) query = query.Where(t => t.Status == status);

        var list = await query.OrderBy(t => t.CreatedAt).ToListAsync(ct);
        return Ok(list.Select(Map));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskItemDto>> GetById(int id, CancellationToken ct)
    {
        var t = await _db.TaskItems.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (t == null) return NotFound();
        return Ok(Map(t));
    }

    [HttpPost]
    public async Task<ActionResult<TaskItemDto>> Create([FromBody] CreateTaskRequest req, CancellationToken ct)
    {
        var project = await _db.Projects.FindAsync([req.ProjectId], ct);
        if (project == null) return BadRequest("Project not found");

        var entity = new TaskItem
        {
            Title = req.Title,
            Description = req.Description ?? "",
            Status = req.Status ?? "todo",
            Priority = req.Priority ?? "medium",
            AssigneeId = req.AssigneeId,
            CreatedBy = req.CreatedBy,
            ProjectId = req.ProjectId,
            SprintId = req.SprintId,
            DueDate = req.DueDate,
            Size = req.Size,
            TagsJson = req.Tags?.Length > 0 ? JsonSerializer.Serialize(req.Tags) : null
        };
        _db.TaskItems.Add(entity);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, Map(entity));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TaskItemDto>> Update(int id, [FromBody] UpdateTaskRequest req, CancellationToken ct)
    {
        var t = await _db.TaskItems.FindAsync([id], ct);
        if (t == null) return NotFound();

        if (req.Title != null) t.Title = req.Title;
        if (req.Description != null) t.Description = req.Description;
        if (req.Status != null) t.Status = req.Status;
        if (req.Priority != null) t.Priority = req.Priority;
        if (req.AssigneeId.HasValue) t.AssigneeId = req.AssigneeId.Value;
        if (req.DueDate.HasValue) t.DueDate = req.DueDate;
        if (req.SprintId.HasValue) t.SprintId = req.SprintId == 0 ? null : req.SprintId;
        if (req.Size.HasValue) t.Size = req.Size;
        if (req.Tags != null) t.TagsJson = req.Tags.Length > 0 ? JsonSerializer.Serialize(req.Tags) : null;

        await _db.SaveChangesAsync(ct);
        return Ok(Map(t));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var t = await _db.TaskItems.FindAsync([id], ct);
        if (t == null) return NotFound();
        _db.TaskItems.Remove(t);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static TaskItemDto Map(TaskItem t) => new(
        t.Id, t.Title, t.Description, t.Status, t.Priority,
        t.AssigneeId, t.CreatedBy, t.CreatedAt, t.DueDate,
        ParseTags(t.TagsJson), t.ProjectId, t.SprintId, t.Size);

    private static string[] ParseTags(string? json)
    {
        if (string.IsNullOrEmpty(json)) return [];
        try { return JsonSerializer.Deserialize<string[]>(json) ?? []; } catch { return []; }
    }
}

public record CreateTaskRequest(string Title, string? Description, string? Status, string? Priority, int AssigneeId, int CreatedBy, int ProjectId, int? SprintId, DateTime? DueDate, string[]? Tags, decimal? Size);
public record UpdateTaskRequest(string? Title, string? Description, string? Status, string? Priority, int? AssigneeId, DateTime? DueDate, int? SprintId, string[]? Tags, decimal? Size);
