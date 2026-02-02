using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskAgent.Contracts.Dtos;
using TaskAgent.DataAccess.Entities;
using TaskAgent.DataAccess.Sql;

namespace TaskAgent.Api.Controllers.TaskManagement;

[ApiController]
[Route("api/tm/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public CommentsController(AppDbContext db) => _db = db;

    [HttpGet("project/{projectId:int}")]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetByProject(int projectId, CancellationToken ct)
    {
        var list = await _db.Comments
            .Include(c => c.Author)
            .Where(c => c.ProjectId == projectId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);
        return Ok(list.Select(Map));
    }

    [HttpGet("task/{taskId:int}")]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetByTask(int taskId, CancellationToken ct)
    {
        var list = await _db.Comments
            .Include(c => c.Author)
            .Where(c => c.TaskId == taskId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);
        return Ok(list.Select(Map));
    }

    [HttpPost]
    public async Task<ActionResult<CommentDto>> Create([FromBody] CreateCommentRequest req, CancellationToken ct)
    {
        if (req.ProjectId.HasValue == req.TaskId.HasValue)
            return BadRequest("Exactly one of ProjectId or TaskId must be set");

        var author = await _db.AppUsers.FindAsync([req.AuthorId], ct);
        if (author == null) return BadRequest("AuthorId not found");

        if (req.ProjectId.HasValue)
        {
            var projectExists = await _db.Projects.AnyAsync(p => p.Id == req.ProjectId.Value, ct);
            if (!projectExists) return BadRequest("Project not found");
        }
        if (req.TaskId.HasValue)
        {
            var taskExists = await _db.TaskItems.AnyAsync(t => t.Id == req.TaskId.Value, ct);
            if (!taskExists) return BadRequest("Task not found");
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
        return Created($"/api/tm/comments/{entity.Id}", Map(entity));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var c = await _db.Comments.FindAsync([id], ct);
        if (c == null) return NotFound();
        _db.Comments.Remove(c);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static CommentDto Map(CommentEntity c) => new(
        c.Id, c.Content, c.AuthorId, c.Author.Name, c.Author.Avatar, c.CreatedAt, c.ProjectId, c.TaskId);
}

public record CreateCommentRequest(string Content, int AuthorId, int? ProjectId, int? TaskId);
