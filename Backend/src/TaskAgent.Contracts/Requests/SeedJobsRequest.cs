using TaskAgent.Contracts.Dtos;

namespace TaskAgent.Contracts.Requests;

/// <summary>
/// Request model for seeding jobs from the generate-jobs.cjs script.
/// </summary>
public record SeedJobsRequest(List<JobDto> Jobs);
