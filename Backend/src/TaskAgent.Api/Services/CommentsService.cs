using Microsoft.EntityFrameworkCore;
using TaskAgent.Contracts.Dtos;
using TaskAgent.Contracts.Requests;
using TaskAgent.DataAccess.Entities;
using TaskAgent.DataAccess.Sql;

namespace TaskAgent.Api.Services;

public class CommentsService : ICommentsService
{
    private readonly AppDbContext _db;

    public CommentsService(AppDbContext db) => _db = db;

    public async Task<IEnumerable<CommentDto>> GetByProjectAsync(int projectId, CancellationToken ct = default)
    {
        var list = await _db.Comments
            .Include(c => c.Author)
            .Where(c => c.ProjectId == projectId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);
        return list.Select(Map);
    }

    public async Task<IEnumerable<CommentDto>> GetByTaskAsync(int taskId, CancellationToken ct = default)
    {
        var list = await _db.Comments
            .Include(c => c.Author)
            .Where(c => c.TaskId == taskId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);
        return list.Select(Map);
    }

    public async Task<(CommentDto? Dto, string? Error)> CreateAsync(CreateCommentRequest req, CancellationToken ct = default)
    {
        if (req.ProjectId.HasValue == req.TaskId.HasValue)
            return (null, "Exactly one of ProjectId or TaskId must be set");

        var author = await _db.AppUsers.FindAsync([req.AuthorId], ct);
        if (author == null) return (null, "AuthorId not found");

        if (req.ProjectId.HasValue)
        {
            var projectExists = await _db.Projects.AnyAsync(p => p.Id == req.ProjectId.Value, ct);
            if (!projectExists) return (null, "Project not found");
        }
        if (req.TaskId.HasValue)
        {
            var taskExists = await _db.TaskItems.AnyAsync(t => t.Id == req.TaskId.Value, ct);
            if (!taskExists) return (null, "Task not found");
        }

        var entity = new CommentEntity
        {
            Content = req.Content.Trim(),
            AuthorId = req.AuthorId,
            ProjectId = req.ProjectId,
            TaskId = req.TaskId
        };
        _db.Comments.Add(entity);
        await _db.SaveChangesAsync(ct);
        entity.Author = author;
        return (Map(entity), null);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var c = await _db.Comments.FindAsync([id], ct);
        if (c == null) return false;
        _db.Comments.Remove(c);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static CommentDto Map(CommentEntity c) => new(
        c.Id, c.Content, c.AuthorId, c.Author.Name, c.Author.Avatar, c.CreatedAt, c.ProjectId, c.TaskId);
}
