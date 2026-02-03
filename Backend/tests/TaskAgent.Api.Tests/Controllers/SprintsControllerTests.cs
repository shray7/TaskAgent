using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskAgent.Api.Controllers.TaskManagement;
using TaskAgent.Api.Services;
using TaskAgent.Contracts.Dtos;
using TaskAgent.Contracts.Requests;
using Xunit;

namespace TaskAgent.Api.Tests.Controllers;

public class SprintsControllerTests
{
    private readonly Mock<ISprintsService> _sprintsMock = new();

    private static SprintsController CreateController(ISprintsService? service = null) => new(service!);

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var dto = new SprintDto(1, 1, "S1", "", DateTime.UtcNow, DateTime.UtcNow.AddDays(14), "planning");
        _sprintsMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        var controller = CreateController(_sprintsMock.Object);

        var result = await controller.GetById(1, default);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _sprintsMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((SprintDto?)null);
        var controller = CreateController(_sprintsMock.Object);

        var result = await controller.GetById(999, default);

        result.Result.Should().BeOfType<NotFoundObjectResult>().Which.Value.Should().BeEquivalentTo(new ApiErrorDto("Sprint not found"));
    }

    [Fact]
    public async Task Create_WhenSuccess_ReturnsCreated()
    {
        var req = new CreateSprintRequest(1, "S1", null, DateTime.UtcNow);
        var dto = new SprintDto(1, 1, "S1", "", DateTime.UtcNow, DateTime.UtcNow.AddDays(14), "planning");
        _sprintsMock.Setup(s => s.CreateAsync(req, It.IsAny<CancellationToken>())).ReturnsAsync((dto, (string?)null));
        var controller = CreateController(_sprintsMock.Object);

        var result = await controller.Create(req, default);

        result.Result.Should().BeOfType<CreatedAtActionResult>().Which.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task Create_WhenError_ReturnsBadRequest()
    {
        var req = new CreateSprintRequest(999, "S1", null, DateTime.UtcNow);
        _sprintsMock.Setup(s => s.CreateAsync(req, It.IsAny<CancellationToken>())).ReturnsAsync((null, "Project not found"));
        var controller = CreateController(_sprintsMock.Object);

        var result = await controller.Create(req, default);

        result.Result.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().BeEquivalentTo(new ApiErrorDto("Project not found"));
    }

    [Fact]
    public async Task Delete_WhenSuccess_ReturnsNoContent()
    {
        _sprintsMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var controller = CreateController(_sprintsMock.Object);

        var result = await controller.Delete(1, default);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        _sprintsMock.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var controller = CreateController(_sprintsMock.Object);

        var result = await controller.Delete(999, default);

        result.Should().BeOfType<NotFoundObjectResult>().Which.Value.Should().BeEquivalentTo(new ApiErrorDto("Sprint not found"));
    }
}
