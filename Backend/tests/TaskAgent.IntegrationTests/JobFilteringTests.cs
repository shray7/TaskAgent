using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaskAgent.Contracts.Dtos;
using TaskAgent.Contracts.Requests;

namespace TaskAgent.IntegrationTests;

/// <summary>
/// Integration tests specifically for job filtering by allowed companies.
/// This is a critical business rule: jobs should only be returned for allowed companies.
/// </summary>
public class JobFilteringTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public JobFilteringTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetJobs_ShouldFilterByAllowedCompanies()
    {
        // Arrange - Create companies with different allowed status
        var uniquePrefix = Guid.NewGuid().ToString("N").Substring(0, 6);
        var allowedCompanyName = $"Allowed-{uniquePrefix}";
        var disallowedCompanyName = $"Disallowed-{uniquePrefix}";

        var companies = new List<CompanyDto>
        {
            new($"AC-{uniquePrefix}", allowedCompanyName, true, "Concrete", "a@a.com", "111", "http://a.com", "Denver", "CO", "CO-1", true, 4.0m, DateTime.UtcNow, null),
            new($"DC-{uniquePrefix}", disallowedCompanyName, false, "Crane", "b@b.com", "222", "http://b.com", "Houston", "TX", "TX-2", false, 4.5m, DateTime.UtcNow, null)
        };
        await _client.PostAsJsonAsync("/api/companies/seed", new SeedCompaniesRequest(companies));

        // Create jobs for both companies
        var jobs = new List<JobDto>
        {
            new($"JA-{uniquePrefix}", "Job Allowed", "Desc", allowedCompanyName, "Denver", "123 St", DateTime.Today.AddHours(9), DateTime.UtcNow.AddDays(1), DateTime.UtcNow, null),
            new($"JD-{uniquePrefix}", "Job Disallowed", "Desc", disallowedCompanyName, "Houston", "456 Ave", DateTime.Today.AddHours(10), DateTime.UtcNow.AddDays(2), DateTime.UtcNow, null)
        };
        await _client.PostAsJsonAsync("/api/jobs/seed", new SeedJobsRequest(jobs));

        // Act - Get filtered jobs
        var response = await _client.GetAsync("/api/jobs");
        var filteredJobs = await response.Content.ReadFromJsonAsync<List<JobDto>>();

        // Assert - Should only contain job from allowed company
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        filteredJobs.Should().Contain(j => j.Id == $"JA-{uniquePrefix}");
        filteredJobs.Should().NotContain(j => j.Id == $"JD-{uniquePrefix}");
    }

    [Fact]
    public async Task GetJobsAll_ShouldReturnAllJobsRegardlessOfCompanyStatus()
    {
        // Arrange
        var uniquePrefix = Guid.NewGuid().ToString("N").Substring(0, 6);
        var allowedCompanyName = $"AllowedAll-{uniquePrefix}";
        var disallowedCompanyName = $"DisallowedAll-{uniquePrefix}";

        var companies = new List<CompanyDto>
        {
            new($"AA-{uniquePrefix}", allowedCompanyName, true, "Concrete", "a@a.com", "111", "http://a.com", "Denver", "CO", "CO-1", true, 4.0m, DateTime.UtcNow, null),
            new($"DA-{uniquePrefix}", disallowedCompanyName, false, "Crane", "b@b.com", "222", "http://b.com", "Houston", "TX", "TX-2", false, 4.5m, DateTime.UtcNow, null)
        };
        await _client.PostAsJsonAsync("/api/companies/seed", new SeedCompaniesRequest(companies));

        var jobs = new List<JobDto>
        {
            new($"JAA-{uniquePrefix}", "Job Allowed All", "Desc", allowedCompanyName, "Denver", "123 St", DateTime.Today.AddHours(9), DateTime.UtcNow.AddDays(1), DateTime.UtcNow, null),
            new($"JDA-{uniquePrefix}", "Job Disallowed All", "Desc", disallowedCompanyName, "Houston", "456 Ave", DateTime.Today.AddHours(10), DateTime.UtcNow.AddDays(2), DateTime.UtcNow, null)
        };
        await _client.PostAsJsonAsync("/api/jobs/seed", new SeedJobsRequest(jobs));

        // Act - Get ALL jobs (unfiltered admin endpoint)
        var response = await _client.GetAsync("/api/jobs/all");
        var allJobs = await response.Content.ReadFromJsonAsync<List<JobDto>>();

        // Assert - Should contain both jobs
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        allJobs.Should().Contain(j => j.Id == $"JAA-{uniquePrefix}");
        allJobs.Should().Contain(j => j.Id == $"JDA-{uniquePrefix}");
    }

    [Fact]
    public async Task GetJobsByCompany_ShouldReturnEmpty_WhenCompanyIsDisallowed()
    {
        // Arrange
        var uniquePrefix = Guid.NewGuid().ToString("N").Substring(0, 6);
        var disallowedCompanyName = $"DisallowedByComp-{uniquePrefix}";

        var company = new CompanyDto(
            $"DBC-{uniquePrefix}",
            disallowedCompanyName,
            false, // Disallowed
            "Hydrovac",
            "d@d.com",
            "333",
            "http://d.com",
            "Phoenix",
            "AZ",
            "AZ-3",
            true,
            3.5m,
            DateTime.UtcNow,
            null);
        await _client.PostAsJsonAsync("/api/companies", company);

        var job = new JobDto(
            $"JDBC-{uniquePrefix}",
            "Job for Disallowed",
            "Desc",
            disallowedCompanyName,
            "Phoenix",
            "789 Blvd",
            DateTime.Today.AddHours(11),
            DateTime.UtcNow.AddDays(3),
            DateTime.UtcNow,
            null);
        await _client.PostAsJsonAsync("/api/jobs", job);

        // Act
        var response = await _client.GetAsync($"/api/jobs/by-company/{Uri.EscapeDataString(disallowedCompanyName)}");
        var jobs = await response.Content.ReadFromJsonAsync<List<JobDto>>();

        // Assert - Should be empty because company is disallowed
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        jobs.Should().BeEmpty();
    }

    [Fact]
    public async Task GetJobsByCompany_ShouldReturnJobs_WhenCompanyIsAllowed()
    {
        // Arrange
        var uniquePrefix = Guid.NewGuid().ToString("N").Substring(0, 6);
        var allowedCompanyName = $"AllowedByComp-{uniquePrefix}";

        var company = new CompanyDto(
            $"ABC-{uniquePrefix}",
            allowedCompanyName,
            true, // Allowed
            "Pipe Services",
            "p@p.com",
            "444",
            "http://p.com",
            "Seattle",
            "WA",
            "WA-4",
            true,
            4.2m,
            DateTime.UtcNow,
            null);
        await _client.PostAsJsonAsync("/api/companies", company);

        var job = new JobDto(
            $"JABC-{uniquePrefix}",
            "Job for Allowed",
            "Desc",
            allowedCompanyName,
            "Seattle",
            "321 Lane",
            DateTime.Today.AddHours(12),
            DateTime.UtcNow.AddDays(4),
            DateTime.UtcNow,
            null);
        await _client.PostAsJsonAsync("/api/jobs", job);

        // Act
        var response = await _client.GetAsync($"/api/jobs/by-company/{Uri.EscapeDataString(allowedCompanyName)}");
        var jobs = await response.Content.ReadFromJsonAsync<List<JobDto>>();

        // Assert - Should contain the job
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        jobs.Should().HaveCount(1);
        jobs!.First().Id.Should().Be($"JABC-{uniquePrefix}");
    }

    [Fact]
    public async Task TogglingCompanyStatus_ShouldAffectJobFiltering()
    {
        // Arrange - Create allowed company with a job
        var uniquePrefix = Guid.NewGuid().ToString("N").Substring(0, 6);
        var companyName = $"ToggleCompany-{uniquePrefix}";
        var companyId = $"TC-{uniquePrefix}";
        var jobId = $"JTC-{uniquePrefix}";

        // Create an additional allowed company to ensure filtering logic is used
        // (prevents fallback to "return all jobs when no companies exist")
        var helperCompany = new CompanyDto(
            $"HELPER-{uniquePrefix}",
            $"HelperCompany-{uniquePrefix}",
            true, // Always allowed
            "Helper",
            "helper@h.com",
            "999",
            "http://helper.com",
            "Helper City",
            "HC",
            "HC-1",
            true,
            5.0m,
            DateTime.UtcNow,
            null);
        await _client.PostAsJsonAsync("/api/companies", helperCompany);

        var company = new CompanyDto(
            companyId,
            companyName,
            true, // Initially allowed
            "General Construction",
            "t@t.com",
            "555",
            "http://t.com",
            "Boston",
            "MA",
            "MA-5",
            true,
            4.0m,
            DateTime.UtcNow,
            null);
        await _client.PostAsJsonAsync("/api/companies", company);

        var job = new JobDto(
            jobId,
            "Toggle Job",
            "Desc",
            companyName,
            "Boston",
            "555 St",
            DateTime.Today.AddHours(13),
            DateTime.UtcNow.AddDays(5),
            DateTime.UtcNow,
            null);
        await _client.PostAsJsonAsync("/api/jobs", job);

        // Act 1 - Verify job is visible when company is allowed
        var response1 = await _client.GetAsync("/api/jobs");
        var jobs1 = await response1.Content.ReadFromJsonAsync<List<JobDto>>();
        jobs1.Should().Contain(j => j.Id == jobId);

        // Act 2 - Disallow the company
        await _client.PutAsync($"/api/companies/{companyId}/disallow", null);

        // Act 3 - Verify job is now hidden
        var response2 = await _client.GetAsync("/api/jobs");
        var jobs2 = await response2.Content.ReadFromJsonAsync<List<JobDto>>();
        jobs2.Should().NotContain(j => j.Id == jobId);

        // Act 4 - Re-allow the company
        await _client.PutAsync($"/api/companies/{companyId}/allow", null);

        // Act 5 - Verify job is visible again
        var response3 = await _client.GetAsync("/api/jobs");
        var jobs3 = await response3.Content.ReadFromJsonAsync<List<JobDto>>();
        jobs3.Should().Contain(j => j.Id == jobId);
    }
}
