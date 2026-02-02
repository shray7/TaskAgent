using FluentAssertions;
using TaskAgent.DataAccess.Entities;

namespace TaskAgent.DataAccess.Tests.Entities;

public class EntityTests
{
    [Fact]
    public void Job_ShouldHaveDefaultValues()
    {
        // Act
        var job = new Job();

        // Assert
        job.Id.Should().BeEmpty();
        job.Title.Should().BeEmpty();
        job.Description.Should().BeEmpty();
        job.Company.Should().BeEmpty();
        job.Location.Should().BeEmpty();
        job.Address.Should().BeEmpty();
        job.ScheduleTime.Should().Be(default(DateTime));
        job.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Job_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var scheduleTime = new DateTime(2026, 2, 15, 9, 0, 0);
        var job = new Job
        {
            Id = "JOB-001",
            Title = "Test Job",
            Description = "Test Description",
            Company = "Test Company",
            Location = "Denver",
            Address = "123 Main St",
            ScheduleTime = scheduleTime,
            ScheduleDate = new DateTime(2026, 2, 15),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        job.Id.Should().Be("JOB-001");
        job.Title.Should().Be("Test Job");
        job.Description.Should().Be("Test Description");
        job.Company.Should().Be("Test Company");
        job.Location.Should().Be("Denver");
        job.Address.Should().Be("123 Main St");
        job.ScheduleTime.Should().Be(scheduleTime);
        job.ScheduleDate.Should().Be(new DateTime(2026, 2, 15));
    }

    [Fact]
    public void Company_ShouldHaveDefaultValues()
    {
        // Act
        var company = new Company();

        // Assert
        company.Id.Should().BeEmpty();
        company.Name.Should().BeEmpty();
        company.IsAllowed.Should().BeTrue(); // Default is true
        company.Industry.Should().BeEmpty();
        company.ContactEmail.Should().BeEmpty();
        company.ContactPhone.Should().BeEmpty();
        company.Website.Should().BeEmpty();
        company.City.Should().BeEmpty();
        company.State.Should().BeEmpty();
        company.LicenseNumber.Should().BeEmpty();
        company.InsuranceVerified.Should().BeFalse();
        company.Rating.Should().BeNull();
        company.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Company_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var company = new Company
        {
            Id = "COMP-001",
            Name = "Test Company",
            IsAllowed = false,
            Industry = "Concrete",
            ContactEmail = "test@company.com",
            ContactPhone = "(555) 123-4567",
            Website = "https://www.testcompany.com",
            City = "Denver",
            State = "CO",
            LicenseNumber = "CO-CON-123456",
            InsuranceVerified = true,
            Rating = 4.5m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        company.Id.Should().Be("COMP-001");
        company.Name.Should().Be("Test Company");
        company.IsAllowed.Should().BeFalse();
        company.Industry.Should().Be("Concrete");
        company.ContactEmail.Should().Be("test@company.com");
        company.ContactPhone.Should().Be("(555) 123-4567");
        company.Website.Should().Be("https://www.testcompany.com");
        company.City.Should().Be("Denver");
        company.State.Should().Be("CO");
        company.LicenseNumber.Should().Be("CO-CON-123456");
        company.InsuranceVerified.Should().BeTrue();
        company.Rating.Should().Be(4.5m);
    }

    [Fact]
    public void User_ShouldHaveDefaultValues()
    {
        // Act
        var user = new User();

        // Assert
        user.Id.Should().NotBeEmpty(); // Auto-generated GUID
        user.Email.Should().BeEmpty();
        user.DisplayName.Should().BeEmpty();
        user.PasswordHash.Should().BeEmpty();
    }

    [Fact]
    public void User_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var user = new User
        {
            Id = "USER-001",
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordHash = "hashed_password",
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        user.Id.Should().Be("USER-001");
        user.Email.Should().Be("test@example.com");
        user.DisplayName.Should().Be("Test User");
        user.PasswordHash.Should().Be("hashed_password");
    }
}
