using FluentValidation.TestHelper;
using TaskAgent.Api.Validators;
using TaskAgent.Contracts.Requests;
using Xunit;

namespace TaskAgent.Api.Tests.Validators;

public class CreateCommentRequestValidatorTests
{
    private readonly CreateCommentRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_ShouldNotHaveErrors()
    {
        var req = new CreateCommentRequest("Comment text", 1, 1, 1);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyContent_ShouldHaveError(string? content)
    {
        var req = new CreateCommentRequest(content!, 1, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void ContentExceeds2000Chars_ShouldHaveError()
    {
        var req = new CreateCommentRequest(new string('x', 2001), 1, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AuthorIdZeroOrNegative_ShouldHaveError(int authorId)
    {
        var req = new CreateCommentRequest("Content", authorId, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.AuthorId);
    }
}
