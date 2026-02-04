using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskAgent.Api.Services;
using TaskAgent.Contracts.Dtos;
using TaskAgent.Contracts.Requests;

namespace TaskAgent.Api.Controllers.TaskManagement;

[ApiController]
[Route("api/tm/[controller]")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly ICommentsService _comments;

    public CommentsController(ICommentsService comments) => _comments = comments;

    [HttpGet("project/{projectId:int}")]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetByProject(int projectId, CancellationToken ct)
    {
        var list = await _comments.GetByProjectAsync(projectId, ct);
        return Ok(list);
    }

    [HttpGet("task/{taskId:int}")]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetByTask(int taskId, CancellationToken ct)
    {
        var list = await _comments.GetByTaskAsync(taskId, ct);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<CommentDto>> Create([FromBody] CreateCommentRequest req, CancellationToken ct)
    {
        var (dto, error) = await _comments.CreateAsync(req, ct);
        if (error != null) return BadRequest(new ApiErrorDto(error));
        return Created($"/api/tm/comments/{dto!.Id}", dto);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!await _comments.DeleteAsync(id, ct)) return NotFound(new ApiErrorDto("Comment not found"));
        return NoContent();
    }
}
