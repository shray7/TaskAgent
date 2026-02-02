using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskAgent.Contracts.Dtos;
using TaskAgent.Contracts.Requests;
using TaskAgent.DataAccess.Entities;
using TaskAgent.DataAccess.Sql;

namespace TaskAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompaniesController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<CompaniesController> _logger;

    public CompaniesController(AppDbContext dbContext, ILogger<CompaniesController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all companies.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CompanyDto>>> GetAll(CancellationToken ct)
    {
        var companies = await _dbContext.Companies.ToListAsync(ct);
        return Ok(companies.Select(MapToDto));
    }

    /// <summary>
    /// Get only allowed companies.
    /// </summary>
    [HttpGet("allowed")]
    public async Task<ActionResult<IEnumerable<CompanyDto>>> GetAllowed(CancellationToken ct)
    {
        var companies = await _dbContext.Companies
            .Where(c => c.IsAllowed)
            .ToListAsync(ct);
        return Ok(companies.Select(MapToDto));
    }

    /// <summary>
    /// Get a company by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CompanyDto>> GetById(string id, CancellationToken ct)
    {
        var company = await _dbContext.Companies.FindAsync([id], ct);
        if (company == null)
            return NotFound();

        return Ok(MapToDto(company));
    }

    /// <summary>
    /// Get a company by name.
    /// </summary>
    [HttpGet("by-name/{name}")]
    public async Task<ActionResult<CompanyDto>> GetByName(string name, CancellationToken ct)
    {
        var company = await _dbContext.Companies
            .FirstOrDefaultAsync(c => c.Name == name, ct);
        if (company == null)
            return NotFound();

        return Ok(MapToDto(company));
    }

    /// <summary>
    /// Get companies by industry.
    /// </summary>
    [HttpGet("by-industry/{industry}")]
    public async Task<ActionResult<IEnumerable<CompanyDto>>> GetByIndustry(string industry, CancellationToken ct)
    {
        var companies = await _dbContext.Companies
            .Where(c => c.Industry == industry)
            .ToListAsync(ct);
        return Ok(companies.Select(MapToDto));
    }

    /// <summary>
    /// Create a new company.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CompanyDto>> Create([FromBody] CompanyDto companyDto, CancellationToken ct)
    {
        var company = MapToEntity(companyDto);
        _dbContext.Companies.Add(company);
        await _dbContext.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = company.Id }, MapToDto(company));
    }

    /// <summary>
    /// Allow a company (set IsAllowed = true).
    /// </summary>
    [HttpPut("{id}/allow")]
    public async Task<ActionResult> Allow(string id, CancellationToken ct)
    {
        var company = await _dbContext.Companies.FindAsync([id], ct);
        if (company == null)
            return NotFound();

        company.IsAllowed = true;
        company.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);
        
        _logger.LogInformation("Company {Id} ({Name}) is now allowed", id, company.Name);
        return Ok(new { message = $"Company '{company.Name}' is now allowed" });
    }

    /// <summary>
    /// Disallow a company (set IsAllowed = false).
    /// </summary>
    [HttpPut("{id}/disallow")]
    public async Task<ActionResult> Disallow(string id, CancellationToken ct)
    {
        var company = await _dbContext.Companies.FindAsync([id], ct);
        if (company == null)
            return NotFound();

        company.IsAllowed = false;
        company.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);
        
        _logger.LogInformation("Company {Id} ({Name}) is now disallowed", id, company.Name);
        return Ok(new { message = $"Company '{company.Name}' is now disallowed" });
    }

    /// <summary>
    /// Seed multiple companies at once (bulk insert).
    /// </summary>
    [HttpPost("seed")]
    public async Task<ActionResult> Seed([FromBody] SeedCompaniesRequest request, CancellationToken ct)
    {
        if (request.Companies == null || request.Companies.Count == 0)
            return BadRequest("No companies provided");

        _logger.LogInformation("Seeding {Count} companies to database", request.Companies.Count);

        var companies = request.Companies.Select(MapToEntity).ToList();
        _dbContext.Companies.AddRange(companies);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Successfully seeded {Count} companies", companies.Count);

        return Ok(new { message = $"Successfully seeded {companies.Count} companies", count = companies.Count });
    }

    /// <summary>
    /// Delete a company by ID.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id, CancellationToken ct)
    {
        var company = await _dbContext.Companies.FindAsync([id], ct);
        if (company == null)
            return NotFound();

        _dbContext.Companies.Remove(company);
        await _dbContext.SaveChangesAsync(ct);
        return NoContent();
    }

    private static CompanyDto MapToDto(Company company) => new(
        company.Id,
        company.Name,
        company.IsAllowed,
        company.Industry,
        company.ContactEmail,
        company.ContactPhone,
        company.Website,
        company.City,
        company.State,
        company.LicenseNumber,
        company.InsuranceVerified,
        company.Rating,
        company.CreatedAt,
        company.UpdatedAt
    );

    private static Company MapToEntity(CompanyDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        IsAllowed = dto.IsAllowed,
        Industry = dto.Industry,
        ContactEmail = dto.ContactEmail,
        ContactPhone = dto.ContactPhone,
        Website = dto.Website,
        City = dto.City,
        State = dto.State,
        LicenseNumber = dto.LicenseNumber,
        InsuranceVerified = dto.InsuranceVerified,
        Rating = dto.Rating,
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt
    };
}
