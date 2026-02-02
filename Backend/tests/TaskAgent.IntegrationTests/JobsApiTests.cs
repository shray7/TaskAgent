using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TaskAgent.Contracts.Dtos;
using TaskAgent.Contracts.Requests;
using TaskAgent.DataAccess.Entities;
using TaskAgent.DataAccess.Sql;

namespace TaskAgent.IntegrationTests;

public class JobsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public JobsApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetJobs_ShouldReturnEmptyList_WhenNoJobsExist()
    {
        // Act
        var response = await _client.GetAsync("/api/jobs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var jobs = await response.Content.ReadFromJsonAsync<List<JobDto>>();
        jobs.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateJob_ShouldReturnCreatedJob()
    {
        // Arrange
        var job = new JobDto(
            $"JOB-{Guid.NewGuid():N}".Substring(0, 10),
            "Test Job",
            "Test Description",
            "Test Company",
            "Denver",
            "123 Test St",
            DateTime.Today.AddHours(9),
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow,
            null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/jobs", job);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdJob = await response.Content.ReadFromJsonAsync<JobDto>();
        createdJob.Should().NotBeNull();
        createdJob!.Title.Should().Be("Test Job");
    }

    [Fact]
    public async Task GetJobById_ShouldReturnJob_WhenExists()
    {
        // Arrange - Create a job first
        var jobId = $"JOB-{Guid.NewGuid():N}".Substring(0, 10);
        var job = new JobDto(
            jobId,
            "Find Me Job",
            "Description",
            "Test Company",
            "Houston",
            "456 Ave",
            DateTime.Today.AddHours(10),
            DateTime.UtcNow.AddDays(5),
            DateTime.UtcNow,
            null);

        await _client.PostAsJsonAsync("/api/jobs", job);

        // Act
        var response = await _client.GetAsync($"/api/jobs/{jobId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var foundJob = await response.Content.ReadFromJsonAsync<JobDto>();
        foundJob.Should().NotBeNull();
        foundJob!.Id.Should().Be(jobId);
        foundJob.Title.Should().Be("Find Me Job");
    }

    [Fact]
    public async Task GetJobById_ShouldReturnNotFound_WhenNotExists()
    {
        // Act
        var response = await _client.GetAsync("/api/jobs/NON-EXISTENT");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SeedJobs_ShouldCreateMultipleJobs()
    {
        // Arrange
        var jobs = new List<JobDto>
        {
            new($"SEED-{Guid.NewGuid():N}".Substring(0, 10), "Seed Job 1", "Desc 1", "Company A", "Denver", "123 St", DateTime.Today.AddHours(9), DateTime.UtcNow.AddDays(1), DateTime.UtcNow, null),
            new($"SEED-{Guid.NewGuid():N}".Substring(0, 10), "Seed Job 2", "Desc 2", "Company B", "Houston", "456 Ave", DateTime.Today.AddHours(10), DateTime.UtcNow.AddDays(2), DateTime.UtcNow, null),
            new($"SEED-{Guid.NewGuid():N}".Substring(0, 10), "Seed Job 3", "Desc 3", "Company C", "Phoenix", "789 Blvd", DateTime.Today.AddHours(11), DateTime.UtcNow.AddDays(3), DateTime.UtcNow, null)
        };
        var request = new SeedJobsRequest(jobs);

        // Act
        var response = await _client.PostAsJsonAsync("/api/jobs/seed", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SeedResponse>();
        result.Should().NotBeNull();
        result!.Count.Should().Be(3);
    }

    [Fact]
    public async Task DeleteJob_ShouldRemoveJob()
    {
        // Arrange - Create a job first
        var jobId = $"DEL-{Guid.NewGuid():N}".Substring(0, 10);
        var job = new JobDto(
            jobId,
            "Delete Me",
            "Description",
            "Test Company",
            "Denver",
            "123 St",
            DateTime.Today.AddHours(9),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow,
            null);

        await _client.PostAsJsonAsync("/api/jobs", job);

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/jobs/{jobId}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's deleted
        var getResponse = await _client.GetAsync($"/api/jobs/{jobId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllUnfiltered_ShouldReturnAllJobs()
    {
        // Arrange
        var jobId = $"ALL-{Guid.NewGuid():N}".Substring(0, 10);
        var job = new JobDto(
            jobId,
            "Unfiltered Job",
            "Description",
            "Random Company",
            "Seattle",
            "999 Way",
            DateTime.Today.AddHours(14),
            DateTime.UtcNow.AddDays(10),
            DateTime.UtcNow,
            null);

        await _client.PostAsJsonAsync("/api/jobs", job);

        // Act
        var response = await _client.GetAsync("/api/jobs/all");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var jobs = await response.Content.ReadFromJsonAsync<List<JobDto>>();
        jobs.Should().NotBeNull();
        jobs.Should().Contain(j => j.Id == jobId);
    }

    private record SeedResponse(string Message, int Count);
}
