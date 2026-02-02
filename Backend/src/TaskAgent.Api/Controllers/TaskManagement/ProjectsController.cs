using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskAgent.Contracts.Dtos;
using TaskAgent.DataAccess.Entities;
using TaskAgent.DataAccess.Sql;

namespace TaskAgent.Api.Controllers.TaskManagement;

[ApiController]
[Route("api/tm/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProjectsController(AppDbContext db) => _db = db;

    /// <summary>Get all active (non-deleted) projects</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetAll(CancellationToken ct)
    {
        var list = await _db.Projects
            .Include(p => p.Owner)
            .Where(p => p.DeletedAt == null)
            .ToListAsync(ct);
        return Ok(list.Select(Map));
    }

    /// <summary>Get project by ID (only if not deleted)</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectDto>> GetById(int id, CancellationToken ct)
    {
        var p = await _db.Projects
            .Include(x => x.Owner)
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (p == null) return NotFound();
        return Ok(Map(p));
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectRequest req, CancellationToken ct)
    {
        var owner = await _db.AppUsers.FindAsync([req.OwnerId], ct);
        if (owner == null) return BadRequest("OwnerId not found");

        var entity = new ProjectEntity
        {
            Name = req.Name,
            Description = req.Description ?? "",
            Color = req.Color ?? "#6366f1",
            OwnerId = req.OwnerId,
            StartDate = req.StartDate,
            EndDate = req.EndDate,
            SprintDurationDays = req.SprintDurationDays ?? 14,
            TaskSizeUnit = req.TaskSizeUnit ?? "hours",
            VisibleToUserIdsJson = req.VisibleToUserIds?.Length > 0 ? JsonSerializer.Serialize(req.VisibleToUserIds) : null,
            VisibleColumnsJson = req.VisibleColumns?.Length > 0 ? JsonSerializer.Serialize(req.VisibleColumns) : null
        };
        _db.Projects.Add(entity);
        await _db.SaveChangesAsync(ct);
        entity.Owner = owner;
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, Map(entity));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProjectDto>> Update(int id, [FromBody] UpdateProjectRequest req, CancellationToken ct)
    {
        var p = await _db.Projects
            .Include(x => x.Owner)
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (p == null) return NotFound();

        if (req.Name != null) p.Name = req.Name;
        if (req.Description != null) p.Description = req.Description;
        if (req.Color != null) p.Color = req.Color;
        if (req.StartDate.HasValue) p.StartDate = req.StartDate;
        if (req.EndDate.HasValue) p.EndDate = req.EndDate;
        if (req.OwnerId.HasValue) p.OwnerId = req.OwnerId.Value;
        if (req.SprintDurationDays.HasValue) p.SprintDurationDays = req.SprintDurationDays.Value;
        if (req.TaskSizeUnit != null) p.TaskSizeUnit = req.TaskSizeUnit;
        if (req.VisibleToUserIds != null) p.VisibleToUserIdsJson = req.VisibleToUserIds.Length > 0 ? JsonSerializer.Serialize(req.VisibleToUserIds) : null;
        if (req.VisibleColumns != null) p.VisibleColumnsJson = req.VisibleColumns.Length > 0 ? JsonSerializer.Serialize(req.VisibleColumns) : null;

        await _db.SaveChangesAsync(ct);
        return Ok(Map(p));
    }

    /// <summary>
    /// Soft-delete a project. Only the project owner can delete.
    /// Also soft-deletes all tasks under this project.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int userId, CancellationToken ct)
    {
        var p = await _db.Projects.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (p == null) return NotFound();
        
        // Only the owner can delete the project
        if (p.OwnerId != userId)
        {
            return Forbid();
        }

        // Soft-delete the project
        p.DeletedAt = DateTime.UtcNow;

        // Soft-delete all tasks under this project
        var tasks = await _db.TaskItems.Where(t => t.ProjectId == id && t.DeletedAt == null).ToListAsync(ct);
        foreach (var task in tasks)
        {
            task.DeletedAt = DateTime.UtcNow;
        }

        // Soft-delete all sprints under this project
        var sprints = await _db.Sprints.Where(s => s.ProjectId == id && s.DeletedAt == null).ToListAsync(ct);
        foreach (var sprint in sprints)
        {
            sprint.DeletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static ProjectDto Map(ProjectEntity p) => new(
        p.Id, p.Name, p.Description, p.Color, p.CreatedAt, p.StartDate, p.EndDate, p.OwnerId,
        ParseIntArray(p.VisibleToUserIdsJson),
        p.SprintDurationDays,
        ParseStringArray(p.VisibleColumnsJson),
        p.TaskSizeUnit);

    private static int[]? ParseIntArray(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<int[]>(json); } catch { return null; }
    }

    private static string[]? ParseStringArray(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<string[]>(json); } catch { return null; }
    }
}

public record CreateProjectRequest(string Name, string? Description, string? Color, int OwnerId, DateTime? StartDate, DateTime? EndDate, int[]? VisibleToUserIds, int? SprintDurationDays, string[]? VisibleColumns, string? TaskSizeUnit);
public record UpdateProjectRequest(string? Name, string? Description, string? Color, int? OwnerId, DateTime? StartDate, DateTime? EndDate, int[]? VisibleToUserIds, int? SprintDurationDays, string[]? VisibleColumns, string? TaskSizeUnit);
