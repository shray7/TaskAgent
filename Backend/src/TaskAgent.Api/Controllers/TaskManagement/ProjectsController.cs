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
public class ProjectsController : ControllerBase
{
    private readonly IProjectsService _projects;

    public ProjectsController(IProjectsService projects) => _projects = projects;

    private int? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(sub, out var id) ? id : null;
    }

    /// <summary>Get all active projects the current user can access (owner or in VisibleToUserIds)</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetAll(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new ApiErrorDto("Invalid token."));
        var list = await _projects.GetAllAsync(userId.Value, ct);
        return Ok(list);
    }

    /// <summary>Get project by ID (only if user has access and not deleted)</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectDto>> GetById(int id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new ApiErrorDto("Invalid token."));
        var p = await _projects.GetByIdAsync(id, userId.Value, ct);
        if (p == null) return NotFound(new ApiErrorDto("Project not found"));
        return Ok(p);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectRequest req, CancellationToken ct)
    {
        var (dto, error) = await _projects.CreateAsync(req, ct);
        if (error != null) return BadRequest(new ApiErrorDto(error));
        return CreatedAtAction(nameof(GetById), new { id = dto!.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProjectDto>> Update(int id, [FromBody] UpdateProjectRequest req, CancellationToken ct)
    {
        var (dto, error) = await _projects.UpdateAsync(id, req, ct);
        if (error != null) return NotFound(new ApiErrorDto("Project not found"));
        return Ok(dto);
    }

    /// <summary>
    /// Soft-delete a project. Only the project owner can delete.
    /// Also soft-deletes all tasks and sprints under this project.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int userId, CancellationToken ct)
    {
        var result = await _projects.DeleteAsync(id, userId, ct);
        if (result == 0) return NotFound(new ApiErrorDto("Project not found"));
        if (result == 1) return StatusCode(403, new ApiErrorDto("Only the project owner can delete this project"));
        return NoContent();
    }
}
