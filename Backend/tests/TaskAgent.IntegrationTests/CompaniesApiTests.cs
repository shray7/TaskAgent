using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaskAgent.Contracts.Dtos;
using TaskAgent.Contracts.Requests;

namespace TaskAgent.IntegrationTests;

public class CompaniesApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public CompaniesApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCompanies_ShouldReturnEmptyList_WhenNoCompaniesExist()
    {
        // Act
        var response = await _client.GetAsync("/api/companies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var companies = await response.Content.ReadFromJsonAsync<List<CompanyDto>>();
        companies.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateCompany_ShouldReturnCreatedCompany()
    {
        // Arrange
        var company = new CompanyDto(
            $"COMP-{Guid.NewGuid():N}".Substring(0, 10),
            "Test Company Inc",
            true,
            "Concrete",
            "test@company.com",
            "(555) 123-4567",
            "https://www.testcompany.com",
            "Denver",
            "CO",
            "CO-CON-123456",
            true,
            4.5m,
            DateTime.UtcNow,
            null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/companies", company);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdCompany = await response.Content.ReadFromJsonAsync<CompanyDto>();
        createdCompany.Should().NotBeNull();
        createdCompany!.Name.Should().Be("Test Company Inc");
    }

    [Fact]
    public async Task GetCompanyById_ShouldReturnCompany_WhenExists()
    {
        // Arrange - Create a company first
        var companyId = $"COMP-{Guid.NewGuid():N}".Substring(0, 10);
        var company = new CompanyDto(
            companyId,
            "Find Me Company",
            true,
            "Crane",
            "find@company.com",
            "(555) 987-6543",
            "https://www.findme.com",
            "Houston",
            "TX",
            "TX-CON-654321",
            true,
            4.8m,
            DateTime.UtcNow,
            null);

        await _client.PostAsJsonAsync("/api/companies", company);

        // Act
        var response = await _client.GetAsync($"/api/companies/{companyId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var foundCompany = await response.Content.ReadFromJsonAsync<CompanyDto>();
        foundCompany.Should().NotBeNull();
        foundCompany!.Id.Should().Be(companyId);
        foundCompany.Name.Should().Be("Find Me Company");
    }

    [Fact]
    public async Task GetCompanyById_ShouldReturnNotFound_WhenNotExists()
    {
        // Act
        var response = await _client.GetAsync("/api/companies/NON-EXISTENT");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SeedCompanies_ShouldCreateMultipleCompanies()
    {
        // Arrange
        var companies = new List<CompanyDto>
        {
            new($"SEED-{Guid.NewGuid():N}".Substring(0, 10), "Seed Company 1", true, "Concrete", "a@a.com", "111", "http://a.com", "Denver", "CO", "CO-1", true, 4.0m, DateTime.UtcNow, null),
            new($"SEED-{Guid.NewGuid():N}".Substring(0, 10), "Seed Company 2", true, "Crane", "b@b.com", "222", "http://b.com", "Houston", "TX", "TX-2", false, 4.5m, DateTime.UtcNow, null),
            new($"SEED-{Guid.NewGuid():N}".Substring(0, 10), "Seed Company 3", false, "Drilling", "c@c.com", "333", "http://c.com", "Phoenix", "AZ", "AZ-3", true, 3.9m, DateTime.UtcNow, null)
        };
        var request = new SeedCompaniesRequest(companies);

        // Act
        var response = await _client.PostAsJsonAsync("/api/companies/seed", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SeedResponse>();
        result.Should().NotBeNull();
        result!.Count.Should().Be(3);
    }

    [Fact]
    public async Task AllowCompany_ShouldSetIsAllowedTrue()
    {
        // Arrange - Create a disallowed company first
        var companyId = $"COMP-{Guid.NewGuid():N}".Substring(0, 10);
        var company = new CompanyDto(
            companyId,
            "Allow Me Company",
            false, // Initially disallowed
            "Hydrovac",
            "allow@company.com",
            "(555) 111-2222",
            "https://www.allowme.com",
            "Seattle",
            "WA",
            "WA-CON-111222",
            true,
            4.2m,
            DateTime.UtcNow,
            null);

        await _client.PostAsJsonAsync("/api/companies", company);

        // Act
        var response = await _client.PutAsync($"/api/companies/{companyId}/allow", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the company is now allowed
        var getResponse = await _client.GetAsync($"/api/companies/{companyId}");
        var updatedCompany = await getResponse.Content.ReadFromJsonAsync<CompanyDto>();
        updatedCompany!.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task DisallowCompany_ShouldSetIsAllowedFalse()
    {
        // Arrange - Create an allowed company first
        var companyId = $"COMP-{Guid.NewGuid():N}".Substring(0, 10);
        var company = new CompanyDto(
            companyId,
            "Disallow Me Company",
            true, // Initially allowed
            "Traffic Control",
            "disallow@company.com",
            "(555) 333-4444",
            "https://www.disallowme.com",
            "Portland",
            "OR",
            "OR-CON-333444",
            false,
            3.8m,
            DateTime.UtcNow,
            null);

        await _client.PostAsJsonAsync("/api/companies", company);

        // Act
        var response = await _client.PutAsync($"/api/companies/{companyId}/disallow", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the company is now disallowed
        var getResponse = await _client.GetAsync($"/api/companies/{companyId}");
        var updatedCompany = await getResponse.Content.ReadFromJsonAsync<CompanyDto>();
        updatedCompany!.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteCompany_ShouldRemoveCompany()
    {
        // Arrange - Create a company first
        var companyId = $"DEL-{Guid.NewGuid():N}".Substring(0, 10);
        var company = new CompanyDto(
            companyId,
            "Delete Me Company",
            true,
            "Hauling",
            "delete@company.com",
            "(555) 555-5555",
            "https://www.deleteme.com",
            "Boston",
            "MA",
            "MA-CON-555555",
            true,
            4.0m,
            DateTime.UtcNow,
            null);

        await _client.PostAsJsonAsync("/api/companies", company);

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/companies/{companyId}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's deleted
        var getResponse = await _client.GetAsync($"/api/companies/{companyId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllowedCompanies_ShouldReturnOnlyAllowedCompanies()
    {
        // Arrange - Create mix of allowed and disallowed companies
        var allowedId = $"ALLOW-{Guid.NewGuid():N}".Substring(0, 11);
        var disallowedId = $"DISAL-{Guid.NewGuid():N}".Substring(0, 11);

        var companies = new List<CompanyDto>
        {
            new(allowedId, "Allowed Company", true, "Concrete", "a@a.com", "111", "http://a.com", "Denver", "CO", "CO-1", true, 4.0m, DateTime.UtcNow, null),
            new(disallowedId, "Disallowed Company", false, "Crane", "b@b.com", "222", "http://b.com", "Houston", "TX", "TX-2", false, 4.5m, DateTime.UtcNow, null)
        };
        var request = new SeedCompaniesRequest(companies);
        await _client.PostAsJsonAsync("/api/companies/seed", request);

        // Act
        var response = await _client.GetAsync("/api/companies/allowed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var allowedCompanies = await response.Content.ReadFromJsonAsync<List<CompanyDto>>();
        allowedCompanies.Should().NotBeNull();
        allowedCompanies.Should().Contain(c => c.Id == allowedId);
        allowedCompanies.Should().NotContain(c => c.Id == disallowedId);
    }

    private record SeedResponse(string Message, int Count);
}
