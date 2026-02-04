using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaskAgent.Contracts.Dtos;
using TaskAgent.Contracts.Requests;
using TaskAgent.DataAccess.Entities;
using TaskAgent.DataAccess.Sql;

namespace TaskAgent.Api.Services;

public class ProjectsService : IProjectsService
{
    private readonly AppDbContext _db;

    public ProjectsService(AppDbContext db) => _db = db;

    public async Task<IEnumerable<ProjectDto>> GetAllAsync(int currentUserId, CancellationToken ct = default)
    {
        var list = await _db.Projects
            .Include(p => p.Owner)
            .Where(p => p.DeletedAt == null)
            .ToListAsync(ct);
        var filtered = list.Where(p => CanUserAccessProject(p, currentUserId));
        return filtered.Select(Map);
    }

    public async Task<ProjectDto?> GetByIdAsync(int id, int currentUserId, CancellationToken ct = default)
    {
        var p = await _db.Projects
            .Include(x => x.Owner)
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (p == null || !CanUserAccessProject(p, currentUserId)) return null;
        return Map(p);
    }

    public async Task<(ProjectDto? Dto, string? Error)> CreateAsync(CreateProjectRequest req, CancellationToken ct = default)
    {
        var owner = await _db.AppUsers.FindAsync([req.OwnerId], ct);
        if (owner == null) return (null, "OwnerId not found");

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
        return (Map(entity), null);
    }

    /// <summary>Conversion factor: 1 work day = 8 hours.</summary>
    private const decimal HoursPerDay = 8m;

    public async Task<(ProjectDto? Dto, string? Error)> UpdateAsync(int id, UpdateProjectRequest req, CancellationToken ct = default)
    {
        var p = await _db.Projects
            .Include(x => x.Owner)
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (p == null) return (null, "Not found");

        var oldTaskSizeUnit = (p.TaskSizeUnit ?? "hours").Trim().ToLowerInvariant();
        var newTaskSizeUnit = req.TaskSizeUnit?.Trim().ToLowerInvariant();

        if (req.TaskSizeUnit != null && newTaskSizeUnit != null && oldTaskSizeUnit != newTaskSizeUnit)
        {
            var tasks = await _db.TaskItems
                .Where(t => t.ProjectId == id && t.DeletedAt == null && t.Size.HasValue && t.Size > 0)
                .ToListAsync(ct);
            foreach (var task in tasks)
            {
                task.Size = ConvertTaskSize(task.Size!.Value, oldTaskSizeUnit, newTaskSizeUnit);
            }
        }

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
        return (Map(p), null);
    }

    /// <summary>Convert a task size value from one unit to another (hours to/from days). Uses 8 hours = 1 day.</summary>
    private static decimal ConvertTaskSize(decimal value, string fromUnit, string toUnit)
    {
        if (fromUnit == toUnit) return value;
        if (fromUnit == "hours" && toUnit == "days")
            return Math.Round(value / HoursPerDay, 2, MidpointRounding.AwayFromZero);
        if (fromUnit == "days" && toUnit == "hours")
            return Math.Round(value * HoursPerDay, 2, MidpointRounding.AwayFromZero);
        return value;
    }

    /// <summary>Soft-delete project and its tasks/sprints. Returns 0=not found, 1=forbidden (not owner), 2=success.</summary>
    public async Task<int> DeleteAsync(int id, int userId, CancellationToken ct = default)
    {
        var p = await _db.Projects.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (p == null) return 0;
        if (p.OwnerId != userId) return 1;

        p.DeletedAt = DateTime.UtcNow;

        var tasks = await _db.TaskItems.Where(t => t.ProjectId == id && t.DeletedAt == null).ToListAsync(ct);
        foreach (var task in tasks) task.DeletedAt = DateTime.UtcNow;

        var sprints = await _db.Sprints.Where(s => s.ProjectId == id && s.DeletedAt == null).ToListAsync(ct);
        foreach (var sprint in sprints) sprint.DeletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return 2;
    }

    private static bool CanUserAccessProject(ProjectEntity p, int userId)
    {
        if (p.OwnerId == userId) return true;
        var visible = ParseIntArray(p.VisibleToUserIdsJson);
        return visible != null && visible.Contains(userId);
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
