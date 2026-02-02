using FluentAssertions;
using TaskAgent.DataAccess.Entities;

namespace TaskAgent.DataAccess.Tests.Entities;

public class EntityTests
{
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
