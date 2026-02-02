using Microsoft.EntityFrameworkCore;
using TaskAgent.Contracts.Dtos;
using TaskAgent.Contracts.Requests;
using TaskAgent.DataAccess.Entities;
using TaskAgent.DataAccess.Sql;

namespace TaskAgent.Api.Services;

public class SprintsService : ISprintsService
{
    private readonly AppDbContext _db;

    public SprintsService(AppDbContext db) => _db = db;

    public async Task<IEnumerable<SprintDto>> GetAllAsync(int? projectId, CancellationToken ct = default)
    {
        var query = _db.Sprints.Where(s => s.DeletedAt == null);
        if (projectId.HasValue)
            query = query.Where(s => s.ProjectId == projectId.Value);
        var list = await query.OrderBy(s => s.StartDate).ToListAsync(ct);
        return list.Select(Map);
    }

    public async Task<SprintDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var s = await _db.Sprints.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        return s == null ? null : Map(s);
    }

    public async Task<(SprintDto? Dto, string? Error)> CreateAsync(CreateSprintRequest req, CancellationToken ct = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == req.ProjectId, ct);
        if (project == null) return (null, "Project not found");

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
        return (Map(entity), null);
    }

    public async Task<(SprintDto? Dto, string? Error)> UpdateAsync(int id, UpdateSprintRequest req, CancellationToken ct = default)
    {
        var s = await _db.Sprints.FindAsync([id], ct);
        if (s == null) return (null, "Not found");

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
        return (Map(s), null);
    }

    public async Task<(SprintDto? Dto, string? Error)> StartAsync(int id, CancellationToken ct = default)
    {
        var s = await _db.Sprints.FindAsync([id], ct);
        if (s == null) return (null, "Not found");
        if (s.Status != "planning") return (null, "Sprint must be in planning to start");

        var active = await _db.Sprints.FirstOrDefaultAsync(x => x.ProjectId == s.ProjectId && x.Status == "active", ct);
        if (active != null) active.Status = "completed";
        s.Status = "active";
        await _db.SaveChangesAsync(ct);
        return (Map(s), null);
    }

    public async Task<(SprintDto? Dto, string? Error)> CompleteAsync(int id, CancellationToken ct = default)
    {
        var s = await _db.Sprints.FindAsync([id], ct);
        if (s == null) return (null, "Not found");
        if (s.Status != "active") return (null, "Sprint must be active to complete");
        s.Status = "completed";
        await _db.SaveChangesAsync(ct);
        return (Map(s), null);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var s = await _db.Sprints.FindAsync([id], ct);
        if (s == null) return false;
        _db.Sprints.Remove(s);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static SprintDto Map(SprintEntity s) => new(s.Id, s.ProjectId, s.Name, s.Goal, s.StartDate, s.EndDate, s.Status);
}
