using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskAgent.Api.Controllers.TaskManagement;
using TaskAgent.Api.Services;
using TaskAgent.Contracts.Dtos;
using Xunit;

namespace TaskAgent.Api.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUsersService> _usersMock = new();

    private static UsersController CreateController(IUsersService? service = null) => new(service!);

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        var expected = new List<AppUserDto> { new(1, "User", "u@example.com", "") };
        _usersMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = CreateController(_usersMock.Object);

        var result = await controller.GetAll(default);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var dto = new AppUserDto(1, "User", "u@example.com", "");
        _usersMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        var controller = CreateController(_usersMock.Object);

        var result = await controller.GetById(1, default);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _usersMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((AppUserDto?)null);
        var controller = CreateController(_usersMock.Object);

        var result = await controller.GetById(999, default);

        result.Result.Should().BeOfType<NotFoundObjectResult>().Which.Value.Should().BeEquivalentTo(new ApiErrorDto("User not found"));
    }

    [Fact]
    public async Task GetByEmail_WhenFound_ReturnsOk()
    {
        var dto = new AppUserDto(1, "User", "u@example.com", "");
        _usersMock.Setup(s => s.GetByEmailAsync("u@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        var controller = CreateController(_usersMock.Object);

        var result = await controller.GetByEmail("u@example.com", default);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetByEmail_WhenNotFound_ReturnsNotFound()
    {
        _usersMock.Setup(s => s.GetByEmailAsync("missing@example.com", It.IsAny<CancellationToken>())).ReturnsAsync((AppUserDto?)null);
        var controller = CreateController(_usersMock.Object);

        var result = await controller.GetByEmail("missing@example.com", default);

        result.Result.Should().BeOfType<NotFoundObjectResult>().Which.Value.Should().BeEquivalentTo(new ApiErrorDto("User not found"));
    }

    [Fact]
    public async Task Register_WhenSuccess_ReturnsOk()
    {
        var req = new RegisterRequest("u@example.com", "User", "password123");
        var response = new AuthResponse(true, null, new AppUserDto(1, "User", "u@example.com", ""));
        _usersMock.Setup(s => s.RegisterAsync(req, It.IsAny<CancellationToken>())).ReturnsAsync(response);
        var controller = CreateController(_usersMock.Object);

        var result = await controller.Register(req, default);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task Register_WhenFails_ReturnsBadRequest()
    {
        var req = new RegisterRequest("u@example.com", "User", "short");
        var response = new AuthResponse(false, "Password must be at least 8 characters", null);
        _usersMock.Setup(s => s.RegisterAsync(req, It.IsAny<CancellationToken>())).ReturnsAsync(response);
        var controller = CreateController(_usersMock.Object);

        var result = await controller.Register(req, default);

        result.Result.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task Login_WhenSuccess_ReturnsOk()
    {
        var req = new LoginRequest("u@example.com", "password");
        var response = new AuthResponse(true, null, new AppUserDto(1, "User", "u@example.com", ""));
        _usersMock.Setup(s => s.LoginAsync(req, It.IsAny<CancellationToken>())).ReturnsAsync(response);
        var controller = CreateController(_usersMock.Object);

        var result = await controller.Login(req, default);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task Login_WhenFails_ReturnsUnauthorized()
    {
        var req = new LoginRequest("u@example.com", "wrong");
        var response = new AuthResponse(false, "Invalid credentials", null);
        _usersMock.Setup(s => s.LoginAsync(req, It.IsAny<CancellationToken>())).ReturnsAsync(response);
        var controller = CreateController(_usersMock.Object);

        var result = await controller.Login(req, default);

        result.Result.Should().BeOfType<UnauthorizedObjectResult>().Which.Value.Should().BeEquivalentTo(response);
    }
}
