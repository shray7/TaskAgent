using FluentValidation.TestHelper;
using TaskAgent.Api.Validators;
using TaskAgent.Contracts.Dtos;
using Xunit;

namespace TaskAgent.Api.Tests.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_ShouldNotHaveErrors()
    {
        var req = new RegisterRequest("user@example.com", "John Doe", "password123");
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EmptyEmail_ShouldHaveError(string? email)
    {
        var req = new RegisterRequest(email!, "Name", "password123");
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void InvalidEmailFormat_ShouldHaveError()
    {
        var req = new RegisterRequest("notanemail", "Name", "password123");
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void NameTooShort_ShouldHaveError()
    {
        var req = new RegisterRequest("a@b.com", "X", "password123");
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void NameTooLong_ShouldHaveError()
    {
        var req = new RegisterRequest("a@b.com", new string('x', 129), "password123");
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void PasswordTooShort_ShouldHaveError()
    {
        var req = new RegisterRequest("a@b.com", "John Doe", "short");
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void EmptyPassword_ShouldHaveError()
    {
        var req = new RegisterRequest("a@b.com", "John Doe", "");
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
