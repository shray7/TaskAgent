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

public class CompaniesControllerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ILogger<CompaniesController>> _loggerMock;
    private readonly CompaniesController _controller;

    public CompaniesControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _loggerMock = new Mock<ILogger<CompaniesController>>();
        _controller = new CompaniesController(_dbContext, _loggerMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetAll_ShouldReturnAllCompanies()
    {
        // Arrange
        await _dbContext.Companies.AddRangeAsync(
            CreateTestCompany("COMP-001", "Company A"),
            CreateTestCompany("COMP-002", "Company B")
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCompanies = okResult.Value.Should().BeAssignableTo<IEnumerable<CompanyDto>>().Subject.ToList();
        
        returnedCompanies.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllowed_ShouldReturnOnlyAllowedCompanies()
    {
        // Arrange
        await _dbContext.Companies.AddRangeAsync(
            CreateTestCompany("COMP-001", "Company A", isAllowed: true),
            CreateTestCompany("COMP-002", "Company B", isAllowed: false),
            CreateTestCompany("COMP-003", "Company C", isAllowed: true)
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetAllowed(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCompanies = okResult.Value.Should().BeAssignableTo<IEnumerable<CompanyDto>>().Subject.ToList();
        
        returnedCompanies.Should().HaveCount(2);
        returnedCompanies.Should().OnlyContain(c => c.IsAllowed);
    }

    [Fact]
    public async Task GetById_ShouldReturnCompany_WhenExists()
    {
        // Arrange
        var company = CreateTestCompany("COMP-001", "Test Company");
        await _dbContext.Companies.AddAsync(company);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetById("COMP-001", CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCompany = okResult.Value.Should().BeOfType<CompanyDto>().Subject;
        
        returnedCompany.Id.Should().Be("COMP-001");
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenNotExists()
    {
        // Act
        var result = await _controller.GetById("COMP-999", CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetByName_ShouldReturnCompany_WhenExists()
    {
        // Arrange
        var company = CreateTestCompany("COMP-001", "Summit Development");
        await _dbContext.Companies.AddAsync(company);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetByName("Summit Development", CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCompany = okResult.Value.Should().BeOfType<CompanyDto>().Subject;
        
        returnedCompany.Name.Should().Be("Summit Development");
    }

    [Fact]
    public async Task GetByIndustry_ShouldReturnCompaniesInIndustry()
    {
        // Arrange
        await _dbContext.Companies.AddRangeAsync(
            CreateTestCompany("COMP-001", "Company A", industry: "Concrete"),
            CreateTestCompany("COMP-002", "Company B", industry: "Crane"),
            CreateTestCompany("COMP-003", "Company C", industry: "Concrete")
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetByIndustry("Concrete", CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCompanies = okResult.Value.Should().BeAssignableTo<IEnumerable<CompanyDto>>().Subject.ToList();
        
        returnedCompanies.Should().HaveCount(2);
    }

    [Fact]
    public async Task Allow_ShouldReturnNotFound_WhenCompanyNotExists()
    {
        // Act
        var result = await _controller.Allow("COMP-999", CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Allow_ShouldSetIsAllowedTrue_WhenCompanyExists()
    {
        // Arrange
        var company = CreateTestCompany("COMP-001", "Test Company", isAllowed: false);
        await _dbContext.Companies.AddAsync(company);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.Allow("COMP-001", CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var updatedCompany = await _dbContext.Companies.FindAsync("COMP-001");
        updatedCompany!.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task Disallow_ShouldSetIsAllowedFalse_WhenCompanyExists()
    {
        // Arrange
        var company = CreateTestCompany("COMP-001", "Test Company", isAllowed: true);
        await _dbContext.Companies.AddAsync(company);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.Disallow("COMP-001", CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var updatedCompany = await _dbContext.Companies.FindAsync("COMP-001");
        updatedCompany!.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedCompany()
    {
        // Arrange
        var companyDto = new CompanyDto(
            "COMP-001", "Test Company", true, "Concrete",
            "test@company.com", "(555) 123-4567", "https://www.test.com",
            "Denver", "CO", "CO-123", true, 4.5m,
            DateTime.UtcNow, null);

        // Act
        var result = await _controller.Create(companyDto, CancellationToken.None);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var returnedCompany = createdResult.Value.Should().BeOfType<CompanyDto>().Subject;
        
        returnedCompany.Id.Should().Be("COMP-001");
        
        var savedCompany = await _dbContext.Companies.FindAsync("COMP-001");
        savedCompany.Should().NotBeNull();
    }

    [Fact]
    public async Task Seed_ShouldReturnBadRequest_WhenNoCompaniesProvided()
    {
        // Arrange
        var request = new SeedCompaniesRequest(new List<CompanyDto>());

        // Act
        var result = await _controller.Seed(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Seed_ShouldAddMultipleCompanies_WhenCompaniesProvided()
    {
        // Arrange
        var companies = new List<CompanyDto>
        {
            new("COMP-001", "Company A", true, "Concrete", "a@a.com", "123", "http://a.com", "Denver", "CO", "CO-1", true, 4.0m, DateTime.UtcNow, null),
            new("COMP-002", "Company B", true, "Crane", "b@b.com", "456", "http://b.com", "Houston", "TX", "TX-2", false, 4.5m, DateTime.UtcNow, null)
        };
        var request = new SeedCompaniesRequest(companies);

        // Act
        var result = await _controller.Seed(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var savedCompanies = await _dbContext.Companies.ToListAsync();
        savedCompanies.Should().HaveCount(2);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenCompanyNotExists()
    {
        // Act
        var result = await _controller.Delete("COMP-999", CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenCompanyDeleted()
    {
        // Arrange
        var company = CreateTestCompany("COMP-001", "Test Company");
        await _dbContext.Companies.AddAsync(company);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.Delete("COMP-001", CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        
        var deletedCompany = await _dbContext.Companies.FindAsync("COMP-001");
        deletedCompany.Should().BeNull();
    }

    private static Company CreateTestCompany(
        string id,
        string name,
        bool isAllowed = true,
        string industry = "General Construction")
    {
        return new Company
        {
            Id = id,
            Name = name,
            IsAllowed = isAllowed,
            Industry = industry,
            ContactEmail = $"info@{name.ToLower().Replace(" ", "")}.com",
            ContactPhone = "(555) 123-4567",
            Website = $"https://www.{name.ToLower().Replace(" ", "")}.com",
            City = "Denver",
            State = "CO",
            LicenseNumber = "CO-CON-123456",
            InsuranceVerified = true,
            Rating = 4.5m,
            CreatedAt = DateTime.UtcNow
        };
    }
}
