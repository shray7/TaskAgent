namespace TaskAgent.Contracts.Dtos;

/// <summary>
/// Company data transfer object.
/// </summary>
public record CompanyDto(
    string Id,
    string Name,
    bool IsAllowed,
    string Industry,
    string ContactEmail,
    string ContactPhone,
    string Website,
    string City,
    string State,
    string LicenseNumber,
    bool InsuranceVerified,
    decimal? Rating,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
