using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TaskAgent.Api.Controllers;
using TaskAgent.Contracts.Dtos;
using TaskAgent.Contracts.Requests;
using TaskAgent.DataAccess.Entities;
using TaskAgent.DataAccess.Sql;

namespace TaskAgent.Api.Tests.Controllers;

public class JobsControllerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ILogger<JobsController>> _loggerMock;
    private readonly JobsController _controller;

    public JobsControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _loggerMock = new Mock<ILogger<JobsController>>();
        _controller = new JobsController(_dbContext, _loggerMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetAll_ShouldReturnFilteredJobs_WhenCompaniesExist()
    {
        // Arrange
        await _dbContext.Companies.AddRangeAsync(
            new Company { Id = "COMP-001", Name = "Company A", IsAllowed = true },
            new Company { Id = "COMP-002", Name = "Company B", IsAllowed = false },
            new Company { Id = "COMP-003", Name = "Company C", IsAllowed = true }
        );
        await _dbContext.Jobs.AddRangeAsync(
            CreateTestJob("JOB-001", "Job 1", "Company A"),
            CreateTestJob("JOB-002", "Job 2", "Company B"),
            CreateTestJob("JOB-003", "Job 3", "Company C")
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedJobs = okResult.Value.Should().BeAssignableTo<IEnumerable<JobDto>>().Subject.ToList();
        
        returnedJobs.Should().HaveCount(2);
        returnedJobs.Should().Contain(j => j.Company == "Company A");
        returnedJobs.Should().Contain(j => j.Company == "Company C");
        returnedJobs.Should().NotContain(j => j.Company == "Company B");
    }

    [Fact]
    public async Task GetAll_ShouldReturnAllJobs_WhenNoCompaniesExist()
    {
        // Arrange
        await _dbContext.Jobs.AddRangeAsync(
            CreateTestJob("JOB-001", "Job 1", "Company A"),
            CreateTestJob("JOB-002", "Job 2", "Company B")
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedJobs = okResult.Value.Should().BeAssignableTo<IEnumerable<JobDto>>().Subject.ToList();
        
        returnedJobs.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllUnfiltered_ShouldReturnAllJobs()
    {
        // Arrange
        await _dbContext.Jobs.AddRangeAsync(
            CreateTestJob("JOB-001", "Job 1", "Company A"),
            CreateTestJob("JOB-002", "Job 2", "Company B")
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetAllUnfiltered(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedJobs = okResult.Value.Should().BeAssignableTo<IEnumerable<JobDto>>().Subject.ToList();
        
        returnedJobs.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_ShouldReturnJob_WhenExists()
    {
        // Arrange
        var job = CreateTestJob("JOB-001", "Test Job", "Company A");
        await _dbContext.Jobs.AddAsync(job);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetById("JOB-001", CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedJob = okResult.Value.Should().BeOfType<JobDto>().Subject;
        
        returnedJob.Id.Should().Be("JOB-001");
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenNotExists()
    {
        // Act
        var result = await _controller.GetById("JOB-999", CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetByCompany_ShouldReturnEmpty_WhenCompanyIsDisallowed()
    {
        // Arrange
        await _dbContext.Companies.AddAsync(new Company { Id = "COMP-001", Name = "Company A", IsAllowed = false });
        await _dbContext.Jobs.AddRangeAsync(
            CreateTestJob("JOB-001", "Job 1", "Company A"),
            CreateTestJob("JOB-002", "Job 2", "Company A")
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetByCompany("Company A", CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedJobs = okResult.Value.Should().BeAssignableTo<IEnumerable<JobDto>>().Subject;
        
        returnedJobs.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByCompany_ShouldReturnJobs_WhenCompanyIsAllowed()
    {
        // Arrange
        await _dbContext.Companies.AddAsync(new Company { Id = "COMP-001", Name = "Company A", IsAllowed = true });
        await _dbContext.Jobs.AddRangeAsync(
            CreateTestJob("JOB-001", "Job 1", "Company A"),
            CreateTestJob("JOB-002", "Job 2", "Company A")
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetByCompany("Company A", CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedJobs = okResult.Value.Should().BeAssignableTo<IEnumerable<JobDto>>().Subject.ToList();
        
        returnedJobs.Should().HaveCount(2);
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedJob()
    {
        // Arrange
        var jobDto = new JobDto(
            "JOB-001", "Test Job", "Description", "Company A",
            "Denver", "123 Main St", DateTime.Today.AddHours(9), DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow, null);

        // Act
        var result = await _controller.Create(jobDto, CancellationToken.None);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var returnedJob = createdResult.Value.Should().BeOfType<JobDto>().Subject;
        
        returnedJob.Id.Should().Be("JOB-001");
        
        var savedJob = await _dbContext.Jobs.FindAsync("JOB-001");
        savedJob.Should().NotBeNull();
    }

    [Fact]
    public async Task Seed_ShouldReturnBadRequest_WhenNoJobsProvided()
    {
        // Arrange
        var request = new SeedJobsRequest(new List<JobDto>());

        // Act
        var result = await _controller.Seed(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Seed_ShouldAddMultipleJobs_WhenJobsProvided()
    {
        // Arrange
        var jobs = new List<JobDto>
        {
            new("JOB-001", "Job 1", "Desc", "Company A", "Denver", "123 St", DateTime.Today.AddHours(9), DateTime.UtcNow, DateTime.UtcNow, null),
            new("JOB-002", "Job 2", "Desc", "Company B", "Houston", "456 Ave", DateTime.Today.AddHours(10), DateTime.UtcNow, DateTime.UtcNow, null)
        };
        var request = new SeedJobsRequest(jobs);

        // Act
        var result = await _controller.Seed(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var savedJobs = await _dbContext.Jobs.ToListAsync();
        savedJobs.Should().HaveCount(2);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenJobNotExists()
    {
        // Act
        var result = await _controller.Delete("JOB-999", CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenJobDeleted()
    {
        // Arrange
        var job = CreateTestJob("JOB-001", "Test Job", "Company A");
        await _dbContext.Jobs.AddAsync(job);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.Delete("JOB-001", CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        
        var deletedJob = await _dbContext.Jobs.FindAsync("JOB-001");
        deletedJob.Should().BeNull();
    }

    private static Job CreateTestJob(string id, string title, string company)
    {
        return new Job
        {
            Id = id,
            Title = title,
            Description = "Test description",
            Company = company,
            Location = "Denver",
            Address = "123 Test St",
            ScheduleTime = DateTime.Today.AddHours(9),
            ScheduleDate = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
    }
}
