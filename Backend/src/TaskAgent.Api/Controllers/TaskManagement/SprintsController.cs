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
public class SprintsController : ControllerBase
{
    private readonly ISprintsService _sprints;

    public SprintsController(ISprintsService sprints) => _sprints = sprints;

    private int? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(sub, out var id) ? id : null;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SprintDto>>> GetAll([FromQuery] int? projectId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new ApiErrorDto("Invalid token."));
        var list = await _sprints.GetAllAsync(userId.Value, projectId, ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SprintDto>> GetById(int id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new ApiErrorDto("Invalid token."));
        var s = await _sprints.GetByIdAsync(id, userId.Value, ct);
        if (s == null) return NotFound(new ApiErrorDto("Sprint not found"));
        return Ok(s);
    }

    [HttpPost]
    public async Task<ActionResult<SprintDto>> Create([FromBody] CreateSprintRequest req, CancellationToken ct)
    {
        var (dto, error) = await _sprints.CreateAsync(req, ct);
        if (error != null) return BadRequest(new ApiErrorDto(error));
        return CreatedAtAction(nameof(GetById), new { id = dto!.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SprintDto>> Update(int id, [FromBody] UpdateSprintRequest req, CancellationToken ct)
    {
        var (dto, error) = await _sprints.UpdateAsync(id, req, ct);
        if (error != null) return NotFound(new ApiErrorDto("Sprint not found"));
        return Ok(dto);
    }

    [HttpPut("{id:int}/start")]
    public async Task<ActionResult<SprintDto>> Start(int id, CancellationToken ct)
    {
        var (dto, error) = await _sprints.StartAsync(id, ct);
        if (error != null) return BadRequest(new ApiErrorDto(error));
        return Ok(dto);
    }

    [HttpPut("{id:int}/complete")]
    public async Task<ActionResult<SprintDto>> Complete(int id, CancellationToken ct)
    {
        var (dto, error) = await _sprints.CompleteAsync(id, ct);
        if (error != null) return BadRequest(new ApiErrorDto(error));
        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!await _sprints.DeleteAsync(id, ct)) return NotFound(new ApiErrorDto("Sprint not found"));
        return NoContent();
    }
}
