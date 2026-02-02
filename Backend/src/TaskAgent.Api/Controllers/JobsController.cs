using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskAgent.Contracts.Dtos;
using TaskAgent.Contracts.Requests;
using TaskAgent.DataAccess.Entities;
using TaskAgent.DataAccess.Sql;

namespace TaskAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<JobsController> _logger;

    public JobsController(AppDbContext dbContext, ILogger<JobsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get jobs from allowed companies only.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobDto>>> GetAll(CancellationToken ct)
    {
        // Get allowed company names
        var allowedCompanies = await _dbContext.Companies
            .Where(c => c.IsAllowed)
            .Select(c => c.Name)
            .ToListAsync(ct);
        
        // If no companies are set up yet, return all jobs (backwards compatibility)
        if (allowedCompanies.Count == 0)
        {
            var allJobs = await _dbContext.Jobs.ToListAsync(ct);
            return Ok(allJobs.Select(MapToDto));
        }

        // Filter jobs to only those from allowed companies
        var jobs = await _dbContext.Jobs
            .Where(j => allowedCompanies.Contains(j.Company))
            .ToListAsync(ct);
        
        return Ok(jobs.Select(MapToDto));
    }

    /// <summary>
    /// Get all jobs without filtering (admin endpoint).
    /// </summary>
    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<JobDto>>> GetAllUnfiltered(CancellationToken ct)
    {
        var jobs = await _dbContext.Jobs.ToListAsync(ct);
        return Ok(jobs.Select(MapToDto));
    }

    /// <summary>
    /// Get a job by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<JobDto>> GetById(string id, CancellationToken ct)
    {
        var job = await _dbContext.Jobs.FindAsync([id], ct);
        if (job == null)
            return NotFound();

        return Ok(MapToDto(job));
    }

    /// <summary>
    /// Get jobs by company name (only if company is allowed).
    /// </summary>
    [HttpGet("by-company/{company}")]
    public async Task<ActionResult<IEnumerable<JobDto>>> GetByCompany(string company, CancellationToken ct)
    {
        // Check if company is allowed
        var companyEntity = await _dbContext.Companies
            .FirstOrDefaultAsync(c => c.Name == company, ct);
        if (companyEntity != null && !companyEntity.IsAllowed)
        {
            return Ok(Enumerable.Empty<JobDto>());
        }

        var jobs = await _dbContext.Jobs
            .Where(j => j.Company == company)
            .ToListAsync(ct);
        return Ok(jobs.Select(MapToDto));
    }

    /// <summary>
    /// Get jobs by location (only from allowed companies).
    /// </summary>
    [HttpGet("by-location/{location}")]
    public async Task<ActionResult<IEnumerable<JobDto>>> GetByLocation(string location, CancellationToken ct)
    {
        var allowedCompanies = await _dbContext.Companies
            .Where(c => c.IsAllowed)
            .Select(c => c.Name)
            .ToListAsync(ct);
        
        var jobs = await _dbContext.Jobs
            .Where(j => j.Location == location)
            .ToListAsync(ct);
        
        // If no companies set up, return all
        if (allowedCompanies.Count == 0)
        {
            return Ok(jobs.Select(MapToDto));
        }

        var filteredJobs = jobs.Where(j => allowedCompanies.Contains(j.Company));
        return Ok(filteredJobs.Select(MapToDto));
    }

    /// <summary>
    /// Create a new job.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<JobDto>> Create([FromBody] JobDto jobDto, CancellationToken ct)
    {
        var job = MapToEntity(jobDto);
        _dbContext.Jobs.Add(job);
        await _dbContext.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = job.Id }, MapToDto(job));
    }

    /// <summary>
    /// Seed multiple jobs at once (bulk insert).
    /// Use this endpoint to import jobs from generate-jobs.cjs script.
    /// </summary>
    [HttpPost("seed")]
    public async Task<ActionResult> Seed([FromBody] SeedJobsRequest request, CancellationToken ct)
    {
        if (request.Jobs == null || request.Jobs.Count == 0)
            return BadRequest("No jobs provided");

        _logger.LogInformation("Seeding {Count} jobs to database", request.Jobs.Count);

        var jobs = request.Jobs.Select(MapToEntity).ToList();
        _dbContext.Jobs.AddRange(jobs);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Successfully seeded {Count} jobs", jobs.Count);

        return Ok(new { message = $"Successfully seeded {jobs.Count} jobs", count = jobs.Count });
    }

    /// <summary>
    /// Delete a job by ID.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id, CancellationToken ct)
    {
        var job = await _dbContext.Jobs.FindAsync([id], ct);
        if (job == null)
            return NotFound();

        _dbContext.Jobs.Remove(job);
        await _dbContext.SaveChangesAsync(ct);
        return NoContent();
    }

    private static JobDto MapToDto(Job job) => new(
        job.Id,
        job.Title,
        job.Description,
        job.Company,
        job.Location,
        job.Address,
        job.ScheduleTime,
        job.ScheduleDate,
        job.CreatedAt,
        job.UpdatedAt
    );

    private static Job MapToEntity(JobDto dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title,
        Description = dto.Description,
        Company = dto.Company,
        Location = dto.Location,
        Address = dto.Address,
        ScheduleTime = dto.ScheduleTime,
        ScheduleDate = dto.ScheduleDate,
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt
    };
}
