using FluentValidation.TestHelper;
using TaskAgent.Api.Validators;
using TaskAgent.Contracts.Dtos;
using Xunit;

namespace TaskAgent.Api.Tests.Validators;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_ShouldNotHaveErrors()
    {
        var req = new LoginRequest("user@example.com", "password123");
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyEmail_ShouldHaveError(string? email)
    {
        var req = new LoginRequest(email!, "password");
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@nodomain.com")]
    [InlineData("missing@")]
    public void InvalidEmailFormat_ShouldHaveError(string email)
    {
        var req = new LoginRequest(email, "password");
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyPassword_ShouldHaveError(string? password)
    {
        var req = new LoginRequest("user@example.com", password!);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
