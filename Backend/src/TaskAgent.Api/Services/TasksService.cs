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

    public async Task<IEnumerable<TaskItemDto>> GetAllAsync(int? projectId, int? sprintId, string? status, CancellationToken ct = default)
    {
        var query = _db.TaskItems.Where(t => t.DeletedAt == null);
        if (projectId.HasValue) query = query.Where(t => t.ProjectId == projectId.Value);
        if (sprintId.HasValue) query = query.Where(t => t.SprintId == sprintId.Value);
        if (!string.IsNullOrEmpty(status)) query = query.Where(t => t.Status == status);

        var list = await query.OrderBy(t => t.CreatedAt).ToListAsync(ct);
        return list.Select(Map);
    }

    public async Task<TaskItemDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var t = await _db.TaskItems.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        return t == null ? null : Map(t);
    }

    public async Task<(TaskItemDto? Dto, string? Error)> CreateAsync(CreateTaskRequest req, CancellationToken ct = default)
    {
        var project = await _db.Projects.FindAsync([req.ProjectId], ct);
        if (project == null) return (null, "Project not found");

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
        return (Map(entity), null);
    }

    public async Task<(TaskItemDto? Dto, string? Error)> UpdateAsync(int id, UpdateTaskRequest req, CancellationToken ct = default)
    {
        var t = await _db.TaskItems.FindAsync([id], ct);
        if (t == null) return (null, "Not found");

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

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var t = await _db.TaskItems.FindAsync([id], ct);
        if (t == null) return false;
        _db.TaskItems.Remove(t);
        await _db.SaveChangesAsync(ct);
        return true;
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
