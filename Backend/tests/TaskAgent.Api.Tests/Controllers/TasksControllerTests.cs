using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskAgent.Api.Controllers.TaskManagement;
using TaskAgent.Api.Services;
using TaskAgent.Contracts.Dtos;
using TaskAgent.Contracts.Requests;
using Xunit;

namespace TaskAgent.Api.Tests.Controllers;

public class TasksControllerTests
{
    private readonly Mock<ITasksService> _tasksMock = new();
    private readonly Mock<IBoardRealtimeNotifier> _realtimeMock = new();

    private static TasksController CreateController(ITasksService? tasks = null, IBoardRealtimeNotifier? realtime = null)
    {
        return new TasksController(tasks!, realtime!);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        var expected = new List<TaskItemDto>
        {
            new(1, "T1", "", "todo", "medium", 1, 1, DateTime.UtcNow, null, [], 1, null, null)
        };
        _tasksMock.Setup(s => s.GetAllAsync(null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = CreateController(_tasksMock.Object, _realtimeMock.Object);

        var result = await controller.GetAll(null, null, null, default);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var dto = new TaskItemDto(1, "T1", "", "todo", "medium", 1, 1, DateTime.UtcNow, null, [], 1, null, null);
        _tasksMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        var controller = CreateController(_tasksMock.Object, _realtimeMock.Object);

        var result = await controller.GetById(1, default);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _tasksMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((TaskItemDto?)null);
        var controller = CreateController(_tasksMock.Object, _realtimeMock.Object);

        var result = await controller.GetById(999, default);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.Value.Should().BeEquivalentTo(new ApiErrorDto("Task not found"));
    }

    [Fact]
    public async Task Create_WhenSuccess_ReturnsCreated()
    {
        var req = new CreateTaskRequest("New", null, null, null, 1, 1, 1, null, null, null, null);
        var dto = new TaskItemDto(1, "New", "", "todo", "medium", 1, 1, DateTime.UtcNow, null, [], 1, null, null);
        _tasksMock.Setup(s => s.CreateAsync(req, It.IsAny<CancellationToken>())).ReturnsAsync((dto, (string?)null));
        var controller = CreateController(_tasksMock.Object, _realtimeMock.Object);

        var result = await controller.Create(req, default);

        result.Result.Should().BeOfType<CreatedAtActionResult>().Which.Value.Should().BeEquivalentTo(dto);
        _realtimeMock.Verify(r => r.NotifyTaskCreatedAsync(dto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenError_ReturnsBadRequest()
    {
        var req = new CreateTaskRequest("New", null, null, null, 1, 1, 1, null, null, null, null);
        _tasksMock.Setup(s => s.CreateAsync(req, It.IsAny<CancellationToken>())).ReturnsAsync((null, "Project not found"));
        var controller = CreateController(_tasksMock.Object, _realtimeMock.Object);

        var result = await controller.Create(req, default);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeEquivalentTo(new ApiErrorDto("Project not found"));
        _realtimeMock.Verify(r => r.NotifyTaskCreatedAsync(It.IsAny<TaskItemDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_WhenSuccess_ReturnsOk()
    {
        var req = new UpdateTaskRequest("Updated", null, null, null, null, null, null, null, null);
        var dto = new TaskItemDto(1, "Updated", "", "todo", "medium", 1, 1, DateTime.UtcNow, null, [], 1, null, null);
        _tasksMock.Setup(s => s.UpdateAsync(1, req, It.IsAny<CancellationToken>())).ReturnsAsync((dto, (string?)null));
        var controller = CreateController(_tasksMock.Object, _realtimeMock.Object);

        var result = await controller.Update(1, req, default);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var req = new UpdateTaskRequest("Updated", null, null, null, null, null, null, null, null);
        _tasksMock.Setup(s => s.UpdateAsync(999, req, It.IsAny<CancellationToken>())).ReturnsAsync((null, "Not found"));
        var controller = CreateController(_tasksMock.Object, _realtimeMock.Object);

        var result = await controller.Update(999, req, default);

        result.Result.Should().BeOfType<NotFoundObjectResult>().Which.Value.Should().BeEquivalentTo(new ApiErrorDto("Task not found"));
    }

    [Fact]
    public async Task Delete_WhenFound_ReturnsNoContent()
    {
        var existing = new TaskItemDto(1, "T", "", "todo", "medium", 1, 1, DateTime.UtcNow, null, [], 1, null, null);
        _tasksMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _tasksMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var controller = CreateController(_tasksMock.Object, _realtimeMock.Object);

        var result = await controller.Delete(1, default);

        result.Should().BeOfType<NoContentResult>();
        _realtimeMock.Verify(r => r.NotifyTaskDeletedAsync(1, null, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        _tasksMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((TaskItemDto?)null);
        var controller = CreateController(_tasksMock.Object, _realtimeMock.Object);

        var result = await controller.Delete(999, default);

        result.Should().BeOfType<NotFoundObjectResult>().Which.Value.Should().BeEquivalentTo(new ApiErrorDto("Task not found"));
    }
}
