using FluentValidation.TestHelper;
using TaskAgent.Api.Validators;
using TaskAgent.Contracts.Requests;
using Xunit;

namespace TaskAgent.Api.Tests.Validators;

public class CreateTaskRequestValidatorTests
{
    private readonly CreateTaskRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_ShouldNotHaveErrors()
    {
        var req = new CreateTaskRequest(
            Title: "Fix bug",
            Description: "Details",
            Status: "todo",
            Priority: "medium",
            AssigneeId: 1,
            CreatedBy: 1,
            ProjectId: 1,
            SprintId: null,
            DueDate: null,
            Tags: null,
            Size: null);

        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyTitle_ShouldHaveError(string? title)
    {
        var req = new CreateTaskRequest(
            title!, "Desc", "todo", "medium", 1, 1, 1, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void TitleExceeds500Chars_ShouldHaveError()
    {
        var req = new CreateTaskRequest(
            Title: new string('x', 501),
            Description: null, Status: null, Priority: null,
            AssigneeId: 1, CreatedBy: 1, ProjectId: 1, SprintId: null, DueDate: null, Tags: null, Size: null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ProjectIdZeroOrNegative_ShouldHaveError(int projectId)
    {
        var req = new CreateTaskRequest(
            "Title", null, null, null, 1, 1, projectId, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData("done")]
    public void InvalidStatus_ShouldHaveError(string status)
    {
        var req = new CreateTaskRequest(
            "Title", null, status, null, 1, 1, 1, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Theory]
    [InlineData("todo")]
    [InlineData("in-progress")]
    [InlineData("completed")]
    public void ValidStatus_ShouldNotHaveError(string status)
    {
        var req = new CreateTaskRequest(
            "Title", null, status, null, 1, 1, 1, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("critical")]
    public void InvalidPriority_ShouldHaveError(string priority)
    {
        var req = new CreateTaskRequest(
            "Title", null, null, priority, 1, 1, 1, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Priority);
    }

    [Theory]
    [InlineData("low")]
    [InlineData("medium")]
    [InlineData("high")]
    public void ValidPriority_ShouldNotHaveError(string priority)
    {
        var req = new CreateTaskRequest(
            "Title", null, null, priority, 1, 1, 1, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Priority);
    }

    [Fact]
    public void NegativeSize_ShouldHaveError()
    {
        var req = new CreateTaskRequest(
            "Title", null, null, null, 1, 1, 1, null, null, null, -1m);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor("Size.Value");
    }

    [Fact]
    public void AssigneeIdZero_ShouldHaveError()
    {
        var req = new CreateTaskRequest(
            "Title", null, null, null, 0, 1, 1, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.AssigneeId);
    }

    [Fact]
    public void CreatedByZero_ShouldHaveError()
    {
        var req = new CreateTaskRequest(
            "Title", null, null, null, 1, 0, 1, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.CreatedBy);
    }
}
