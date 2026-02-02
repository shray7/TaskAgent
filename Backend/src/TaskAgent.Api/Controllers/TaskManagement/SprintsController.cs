using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskAgent.Contracts.Dtos;
using TaskAgent.DataAccess.Entities;
using TaskAgent.DataAccess.Sql;

namespace TaskAgent.Api.Controllers.TaskManagement;

[ApiController]
[Route("api/tm/[controller]")]
public class SprintsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SprintsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SprintDto>>> GetAll([FromQuery] int? projectId, CancellationToken ct)
    {
        var query = _db.Sprints.Where(s => s.DeletedAt == null);
        if (projectId.HasValue)
            query = query.Where(s => s.ProjectId == projectId.Value);
        var list = await query.OrderBy(s => s.StartDate).ToListAsync(ct);
        return Ok(list.Select(Map));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SprintDto>> GetById(int id, CancellationToken ct)
    {
        var s = await _db.Sprints.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (s == null) return NotFound();
        return Ok(Map(s));
    }

    [HttpPost]
    public async Task<ActionResult<SprintDto>> Create([FromBody] CreateSprintRequest req, CancellationToken ct)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == req.ProjectId, ct);
        if (project == null) return BadRequest("Project not found");

        var durationDays = project.SprintDurationDays;
        var endDate = req.StartDate.AddDays(durationDays);

        var entity = new SprintEntity
        {
            ProjectId = req.ProjectId,
            Name = req.Name,
            Goal = req.Goal ?? "",
            StartDate = req.StartDate,
            EndDate = endDate,
            Status = "planning"
        };
        _db.Sprints.Add(entity);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, Map(entity));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SprintDto>> Update(int id, [FromBody] UpdateSprintRequest req, CancellationToken ct)
    {
        var s = await _db.Sprints.FindAsync([id], ct);
        if (s == null) return NotFound();

        if (req.Name != null) s.Name = req.Name;
        if (req.Goal != null) s.Goal = req.Goal;
        if (req.StartDate.HasValue)
        {
            s.StartDate = req.StartDate.Value;
            var project = await _db.Projects.FindAsync([s.ProjectId], ct);
            s.EndDate = req.EndDate ?? s.StartDate.AddDays(project?.SprintDurationDays ?? 14);
        }
        if (req.EndDate.HasValue) s.EndDate = req.EndDate.Value;
        if (req.Status != null) s.Status = req.Status;

        await _db.SaveChangesAsync(ct);
        return Ok(Map(s));
    }

    [HttpPut("{id:int}/start")]
    public async Task<ActionResult<SprintDto>> Start(int id, CancellationToken ct)
    {
        var s = await _db.Sprints.FindAsync([id], ct);
        if (s == null) return NotFound();
        if (s.Status != "planning") return BadRequest("Sprint must be in planning to start");

        var active = await _db.Sprints.FirstOrDefaultAsync(x => x.ProjectId == s.ProjectId && x.Status == "active", ct);
        if (active != null) active.Status = "completed";
        s.Status = "active";
        await _db.SaveChangesAsync(ct);
        return Ok(Map(s));
    }

    [HttpPut("{id:int}/complete")]
    public async Task<ActionResult<SprintDto>> Complete(int id, CancellationToken ct)
    {
        var s = await _db.Sprints.FindAsync([id], ct);
        if (s == null) return NotFound();
        if (s.Status != "active") return BadRequest("Sprint must be active to complete");
        s.Status = "completed";
        await _db.SaveChangesAsync(ct);
        return Ok(Map(s));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var s = await _db.Sprints.FindAsync([id], ct);
        if (s == null) return NotFound();
        _db.Sprints.Remove(s);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static SprintDto Map(SprintEntity s) => new(s.Id, s.ProjectId, s.Name, s.Goal, s.StartDate, s.EndDate, s.Status);
}

public record CreateSprintRequest(int ProjectId, string Name, string? Goal, DateTime StartDate);
public record UpdateSprintRequest(string? Name, string? Goal, DateTime? StartDate, DateTime? EndDate, string? Status);
