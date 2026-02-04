using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaskAgent.Contracts.Dtos;
using TaskAgent.Contracts.Requests;
using TaskAgent.DataAccess.Entities;
using TaskAgent.DataAccess.Sql;

namespace TaskAgent.Api.Services;

public class TasksService : ITasksService
{
    private readonly AppDbContext _db;

    public TasksService(AppDbContext db) => _db = db;

    public async Task<IEnumerable<TaskItemDto>> GetAllAsync(int currentUserId, int? projectId, int? sprintId, string? status, CancellationToken ct = default)
    {
        var accessibleProjectIds = await GetAccessibleProjectIdsAsync(currentUserId, ct);
        var query = _db.TaskItems.Where(t => t.DeletedAt == null && accessibleProjectIds.Contains(t.ProjectId));
        if (projectId.HasValue)
        {
            if (!accessibleProjectIds.Contains(projectId.Value))
                return Array.Empty<TaskItemDto>();
            query = query.Where(t => t.ProjectId == projectId.Value);
        }
        if (sprintId.HasValue) query = query.Where(t => t.SprintId == sprintId.Value);
        if (!string.IsNullOrEmpty(status)) query = query.Where(t => t.Status == status);

        var list = await query.OrderBy(t => t.CreatedAt).ToListAsync(ct);
        return list.Select(Map);
    }

    public async Task<TaskItemDto?> GetByIdAsync(int id, int currentUserId, CancellationToken ct = default)
    {
        var t = await _db.TaskItems.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (t == null) return null;
        if (!await CanUserAccessProjectAsync(currentUserId, t.ProjectId, ct))
            return null;
        return Map(t);
    }

    public async Task<(TaskItemDto? Dto, string? Error)> CreateAsync(CreateTaskRequest req, int currentUserId, CancellationToken ct = default)
    {
        var project = await _db.Projects.FindAsync([req.ProjectId], ct);
        if (project == null) return (null, "Project not found");
        if (!await CanUserAccessProjectAsync(currentUserId, project.Id, ct))
            return (null, "You do not have access to this project.");

        var entity = new TaskItem
        {
            Title = req.Title,
            Description = req.Description ?? "",
            Status = req.Status ?? "todo",
            Priority = req.Priority ?? "medium",
            AssigneeId = req.AssigneeId,
            CreatedBy = currentUserId,
            ProjectId = req.ProjectId,
            SprintId = req.SprintId,
            DueDate = req.DueDate,
            Size = req.Size,
            TagsJson = req.Tags?.Length > 0 ? JsonSerializer.Serialize(req.Tags) : null
        };
        _db.TaskItems.Add(entity);
        await _db.SaveChangesAsync(ct);
        return (Map(entity), null);
    }

    public async Task<(TaskItemDto? Dto, string? Error)> UpdateAsync(int id, UpdateTaskRequest req, int currentUserId, CancellationToken ct = default)
    {
        var t = await _db.TaskItems.FindAsync([id], ct);
        if (t == null) return (null, "Not found");
        if (!await CanUserAccessProjectAsync(currentUserId, t.ProjectId, ct))
            return (null, "You do not have access to this project.");

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
        return (Map(t), null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, int currentUserId, CancellationToken ct = default)
    {
        var t = await _db.TaskItems.FindAsync([id], ct);
        if (t == null) return (false, "Not found");
        if (!await CanUserAccessProjectAsync(currentUserId, t.ProjectId, ct))
            return (false, "You do not have access to this project.");
        _db.TaskItems.Remove(t);
        await _db.SaveChangesAsync(ct);
        return (true, null);
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
            if (visible != null && visible.Contains(userId))
                ids.Add(p.Id);
        }
        return ids;
    }

    private static int[]? ParseIntArray(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<int[]>(json); } catch { return null; }
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
