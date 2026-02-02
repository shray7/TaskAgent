using Microsoft.AspNetCore.Mvc;
using TaskAgent.Api.Services;
using TaskAgent.Contracts.Dtos;
using TaskAgent.Contracts.Requests;

namespace TaskAgent.Api.Controllers.TaskManagement;

[ApiController]
[Route("api/tm/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectsService _projects;

    public ProjectsController(IProjectsService projects) => _projects = projects;

    /// <summary>Get all active (non-deleted) projects</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetAll(CancellationToken ct)
    {
        var list = await _projects.GetAllAsync(ct);
        return Ok(list);
    }

    /// <summary>Get project by ID (only if not deleted)</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectDto>> GetById(int id, CancellationToken ct)
    {
        var p = await _projects.GetByIdAsync(id, ct);
        if (p == null) return NotFound();
        return Ok(p);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectRequest req, CancellationToken ct)
    {
        var (dto, error) = await _projects.CreateAsync(req, ct);
        if (error != null) return BadRequest(error);
        return CreatedAtAction(nameof(GetById), new { id = dto!.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProjectDto>> Update(int id, [FromBody] UpdateProjectRequest req, CancellationToken ct)
    {
        var (dto, error) = await _projects.UpdateAsync(id, req, ct);
        if (error != null) return NotFound();
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
        if (result == 0) return NotFound();
        if (result == 1) return Forbid();
        return NoContent();
    }
}
