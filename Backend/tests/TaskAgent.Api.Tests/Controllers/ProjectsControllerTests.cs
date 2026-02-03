using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskAgent.Api.Controllers.TaskManagement;
using TaskAgent.Api.Services;
using TaskAgent.Contracts.Dtos;
using TaskAgent.Contracts.Requests;
using Xunit;

namespace TaskAgent.Api.Tests.Controllers;

public class ProjectsControllerTests
{
    private readonly Mock<IProjectsService> _projectsMock = new();

    private static ProjectsController CreateController(IProjectsService? service = null) =>
        new(service!);

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        var expected = new List<ProjectDto>
        {
            new(1, "P1", "", "#6366f1", DateTime.UtcNow, null, null, 1, null, 14, null, "hours")
        };
        _projectsMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = CreateController(_projectsMock.Object);

        var result = await controller.GetAll(default);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var dto = new ProjectDto(1, "P1", "", "#6366f1", DateTime.UtcNow, null, null, 1, null, 14, null, "hours");
        _projectsMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        var controller = CreateController(_projectsMock.Object);

        var result = await controller.GetById(1, default);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _projectsMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((ProjectDto?)null);
        var controller = CreateController(_projectsMock.Object);

        var result = await controller.GetById(999, default);

        result.Result.Should().BeOfType<NotFoundObjectResult>().Which.Value.Should().BeEquivalentTo(new ApiErrorDto("Project not found"));
    }

    [Fact]
    public async Task Create_WhenSuccess_ReturnsCreated()
    {
        var req = new CreateProjectRequest("P1", null, null, 1, null, null, null, null, null, null);
        var dto = new ProjectDto(1, "P1", "", "#6366f1", DateTime.UtcNow, null, null, 1, null, 14, null, "hours");
        _projectsMock.Setup(s => s.CreateAsync(req, It.IsAny<CancellationToken>())).ReturnsAsync((dto, (string?)null));
        var controller = CreateController(_projectsMock.Object);

        var result = await controller.Create(req, default);

        result.Result.Should().BeOfType<CreatedAtActionResult>().Which.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task Create_WhenError_ReturnsBadRequest()
    {
        var req = new CreateProjectRequest("P1", null, null, 999, null, null, null, null, null, null);
        _projectsMock.Setup(s => s.CreateAsync(req, It.IsAny<CancellationToken>())).ReturnsAsync((null, "OwnerId not found"));
        var controller = CreateController(_projectsMock.Object);

        var result = await controller.Create(req, default);

        result.Result.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().BeEquivalentTo(new ApiErrorDto("OwnerId not found"));
    }

    [Fact]
    public async Task Delete_WhenSuccess_ReturnsNoContent()
    {
        _projectsMock.Setup(s => s.DeleteAsync(1, 1, It.IsAny<CancellationToken>())).ReturnsAsync(2);
        var controller = CreateController(_projectsMock.Object);

        var result = await controller.Delete(1, 1, default);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WhenProjectNotFound_ReturnsNotFound()
    {
        _projectsMock.Setup(s => s.DeleteAsync(999, 1, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        var controller = CreateController(_projectsMock.Object);

        var result = await controller.Delete(999, 1, default);

        result.Should().BeOfType<NotFoundObjectResult>().Which.Value.Should().BeEquivalentTo(new ApiErrorDto("Project not found"));
    }

    [Fact]
    public async Task Delete_WhenNotOwner_Returns403()
    {
        _projectsMock.Setup(s => s.DeleteAsync(1, 2, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var controller = CreateController(_projectsMock.Object);

        var result = await controller.Delete(1, 2, default);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
        ((ObjectResult)result).Value.Should().BeEquivalentTo(new ApiErrorDto("Only the project owner can delete this project"));
    }
}
