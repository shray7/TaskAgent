using FluentValidation.TestHelper;
using TaskAgent.Api.Validators;
using TaskAgent.Contracts.Requests;
using Xunit;

namespace TaskAgent.Api.Tests.Validators;

public class UpdateTaskRequestValidatorTests
{
    private readonly UpdateTaskRequestValidator _validator = new();

    [Fact]
    public void AllNullOptionalFields_ShouldNotHaveErrors()
    {
        var req = new UpdateTaskRequest(null, null, null, null, null, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ValidTitle_ShouldNotHaveError()
    {
        var req = new UpdateTaskRequest("New title", null, null, null, null, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyTitle_ShouldHaveError(string title)
    {
        var req = new UpdateTaskRequest(title, null, null, null, null, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void TitleExceeds500Chars_ShouldHaveError()
    {
        var req = new UpdateTaskRequest(new string('x', 501), null, null, null, null, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Theory]
    [InlineData("todo")]
    [InlineData("in-progress")]
    [InlineData("completed")]
    public void ValidStatus_ShouldNotHaveError(string status)
    {
        var req = new UpdateTaskRequest(null, null, status, null, null, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void InvalidStatus_ShouldHaveError()
    {
        var req = new UpdateTaskRequest(null, null, "invalid", null, null, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Theory]
    [InlineData("low")]
    [InlineData("medium")]
    [InlineData("high")]
    public void ValidPriority_ShouldNotHaveError(string priority)
    {
        var req = new UpdateTaskRequest(null, null, null, priority, null, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Priority);
    }

    [Fact]
    public void InvalidPriority_ShouldHaveError()
    {
        var req = new UpdateTaskRequest(null, null, null, "critical", null, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Priority);
    }

    [Fact]
    public void NegativeSize_ShouldHaveError()
    {
        var req = new UpdateTaskRequest(null, null, null, null, null, null, null, null, -1m);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor("Size.Value");
    }
}
