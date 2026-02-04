using System.Text.Json;
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

    public async Task<IEnumerable<SprintDto>> GetAllAsync(int currentUserId, int? projectId, CancellationToken ct = default)
    {
        var accessibleProjectIds = await GetAccessibleProjectIdsAsync(currentUserId, ct);
        var query = _db.Sprints.Where(s => s.DeletedAt == null && accessibleProjectIds.Contains(s.ProjectId));
        if (projectId.HasValue)
        {
            if (!accessibleProjectIds.Contains(projectId.Value)) return Array.Empty<SprintDto>();
            query = query.Where(s => s.ProjectId == projectId.Value);
        }
        var list = await query.OrderBy(s => s.StartDate).ToListAsync(ct);
        return list.Select(Map);
    }

    public async Task<SprintDto?> GetByIdAsync(int id, int currentUserId, CancellationToken ct = default)
    {
        var s = await _db.Sprints.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (s == null) return null;
        if (!await CanUserAccessProjectAsync(currentUserId, s.ProjectId, ct)) return null;
        return Map(s);
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

    private async Task<bool> CanUserAccessProjectAsync(int userId, int projectId, CancellationToken ct)
    {
        var project = await _db.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId && p.DeletedAt == null, ct);
        if (project == null) return false;
        if (project.OwnerId == userId) return true;
        var visible = ParseIntArray(project.VisibleToUserIdsJson);
        return visible != null && visible.Contains(userId);
    }

    private async Task<List<int>> GetAccessibleProjectIdsAsync(int userId, CancellationToken ct)
    {
        var projects = await _db.Projects.AsNoTracking()
            .Where(p => p.DeletedAt == null)
            .Select(p => new { p.Id, p.OwnerId, p.VisibleToUserIdsJson })
            .ToListAsync(ct);
        var ids = new List<int>();
        foreach (var p in projects)
        {
            if (p.OwnerId == userId) { ids.Add(p.Id); continue; }
            var visible = ParseIntArray(p.VisibleToUserIdsJson);
            if (visible != null && visible.Contains(userId)) ids.Add(p.Id);
        }
        return ids;
    }

    private static int[]? ParseIntArray(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<int[]>(json); } catch { return null; }
    }

    private static SprintDto Map(SprintEntity s) => new(s.Id, s.ProjectId, s.Name, s.Goal, s.StartDate, s.EndDate, s.Status);
}
