using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskAgent.Api.Services;
using TaskAgent.Contracts.Dtos;
using TaskAgent.Contracts.Requests;

namespace TaskAgent.Api.Controllers.TaskManagement;

[ApiController]
[Route("api/tm/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITasksService _tasks;
    private readonly IBoardRealtimeNotifier _realtime;

    public TasksController(ITasksService tasks, IBoardRealtimeNotifier realtime)
    {
        _tasks = tasks;
        _realtime = realtime;
    }

    private int? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(sub, out var id) ? id : null;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskItemDto>>> GetAll(
        [FromQuery] int? projectId,
        [FromQuery] int? sprintId,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new ApiErrorDto("Invalid token."));
        var list = await _tasks.GetAllAsync(userId.Value, projectId, sprintId, status, ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskItemDto>> GetById(int id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new ApiErrorDto("Invalid token."));
        var t = await _tasks.GetByIdAsync(id, userId.Value, ct);
        if (t == null) return NotFound(new ApiErrorDto("Task not found"));
        return Ok(t);
    }

    [HttpPost]
    public async Task<ActionResult<TaskItemDto>> Create([FromBody] CreateTaskRequest req, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new ApiErrorDto("Invalid token."));
        var (dto, error) = await _tasks.CreateAsync(req, userId.Value, ct);
        if (error != null)
            return error == "Project not found" ? NotFound(new ApiErrorDto(error)) : StatusCode(403, new ApiErrorDto(error));
        _ = _realtime.NotifyTaskCreatedAsync(dto!, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto!.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TaskItemDto>> Update(int id, [FromBody] UpdateTaskRequest req, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new ApiErrorDto("Invalid token."));
        var (dto, error) = await _tasks.UpdateAsync(id, req, userId.Value, ct);
        if (error != null)
            return error == "Not found" ? NotFound(new ApiErrorDto("Task not found")) : StatusCode(403, new ApiErrorDto(error));
        _ = _realtime.NotifyTaskUpdatedAsync(dto!, ct);
        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new ApiErrorDto("Invalid token."));
        var existing = await _tasks.GetByIdAsync(id, userId.Value, ct);
        if (existing == null) return NotFound(new ApiErrorDto("Task not found"));
        var projectId = existing.ProjectId;
        var sprintId = existing.SprintId;
        var (success, error) = await _tasks.DeleteAsync(id, userId.Value, ct);
        if (!success)
            return error == "Not found" ? NotFound(new ApiErrorDto("Task not found")) : StatusCode(403, new ApiErrorDto(error!));
        _ = _realtime.NotifyTaskDeletedAsync(projectId, sprintId, id, ct);
        return NoContent();
    }
}
