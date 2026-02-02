using TaskAgent.Contracts.Dtos;

namespace TaskAgent.Contracts.Requests;

/// <summary>
/// Request model for seeding companies.
/// </summary>
public record SeedCompaniesRequest(List<CompanyDto> Companies);
