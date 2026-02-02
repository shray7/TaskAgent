namespace TaskAgent.DataAccess.Entities;

/// <summary>
/// Company entity for database storage.
/// Represents a subcontractor/vendor company that can have jobs.
/// </summary>
public class Company
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsAllowed { get; set; } = true;
    public string Industry { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public bool InsuranceVerified { get; set; }
    public decimal? Rating { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
