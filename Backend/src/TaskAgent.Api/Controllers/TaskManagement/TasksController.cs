using Microsoft.AspNetCore.Mvc;
using TaskAgent.Api.Services;
using TaskAgent.Contracts.Dtos;
using TaskAgent.Contracts.Requests;

namespace TaskAgent.Api.Controllers.TaskManagement;

[ApiController]
[Route("api/tm/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITasksService _tasks;

    public TasksController(ITasksService tasks) => _tasks = tasks;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskItemDto>>> GetAll(
        [FromQuery] int? projectId,
        [FromQuery] int? sprintId,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var list = await _tasks.GetAllAsync(projectId, sprintId, status, ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskItemDto>> GetById(int id, CancellationToken ct)
    {
        var t = await _tasks.GetByIdAsync(id, ct);
        if (t == null) return NotFound();
        return Ok(t);
    }

    [HttpPost]
    public async Task<ActionResult<TaskItemDto>> Create([FromBody] CreateTaskRequest req, CancellationToken ct)
    {
        var (dto, error) = await _tasks.CreateAsync(req, ct);
        if (error != null) return BadRequest(error);
        return CreatedAtAction(nameof(GetById), new { id = dto!.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TaskItemDto>> Update(int id, [FromBody] UpdateTaskRequest req, CancellationToken ct)
    {
        var (dto, error) = await _tasks.UpdateAsync(id, req, ct);
        if (error != null) return NotFound();
        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!await _tasks.DeleteAsync(id, ct)) return NotFound();
        return NoContent();
    }
}
