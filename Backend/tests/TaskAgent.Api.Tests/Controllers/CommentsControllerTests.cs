using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskAgent.Api.Controllers.TaskManagement;
using TaskAgent.Api.Services;
using TaskAgent.Contracts.Dtos;
using TaskAgent.Contracts.Requests;
using Xunit;

namespace TaskAgent.Api.Tests.Controllers;

public class CommentsControllerTests
{
    private readonly Mock<ICommentsService> _commentsMock = new();

    private static CommentsController CreateController(ICommentsService? service = null) => new(service!);

    [Fact]
    public async Task GetByProject_ReturnsOkWithList()
    {
        var expected = new List<CommentDto>
        {
            new(1, "Comment", 1, "User", "", DateTime.UtcNow, 1, null)
        };
        _commentsMock.Setup(s => s.GetByProjectAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = CreateController(_commentsMock.Object);

        var result = await controller.GetByProject(1, default);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetByTask_ReturnsOkWithList()
    {
        var expected = new List<CommentDto>
        {
            new(1, "Comment", 1, "User", "", DateTime.UtcNow, null, 1)
        };
        _commentsMock.Setup(s => s.GetByTaskAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = CreateController(_commentsMock.Object);

        var result = await controller.GetByTask(1, default);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Create_WhenSuccess_ReturnsCreated()
    {
        var req = new CreateCommentRequest("Hello", 1, 1, 1);
        var dto = new CommentDto(1, "Hello", 1, "User", "", DateTime.UtcNow, 1, 1);
        _commentsMock.Setup(s => s.CreateAsync(req, It.IsAny<CancellationToken>())).ReturnsAsync((dto, (string?)null));
        var controller = CreateController(_commentsMock.Object);

        var result = await controller.Create(req, default);

        result.Result.Should().BeOfType<CreatedResult>().Which.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task Create_WhenError_ReturnsBadRequest()
    {
        var req = new CreateCommentRequest("Hello", 1, 1, 999);
        _commentsMock.Setup(s => s.CreateAsync(req, It.IsAny<CancellationToken>())).ReturnsAsync((null, "Task not found"));
        var controller = CreateController(_commentsMock.Object);

        var result = await controller.Create(req, default);

        result.Result.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().BeEquivalentTo(new ApiErrorDto("Task not found"));
    }

    [Fact]
    public async Task Delete_WhenSuccess_ReturnsNoContent()
    {
        _commentsMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var controller = CreateController(_commentsMock.Object);

        var result = await controller.Delete(1, default);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        _commentsMock.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var controller = CreateController(_commentsMock.Object);

        var result = await controller.Delete(999, default);

        result.Should().BeOfType<NotFoundObjectResult>().Which.Value.Should().BeEquivalentTo(new ApiErrorDto("Comment not found"));
    }
}
