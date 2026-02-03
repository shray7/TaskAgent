using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using TaskAgent.Api.Filters;
using TaskAgent.Contracts.Dtos;
using TaskAgent.Contracts.Requests;
using Xunit;

namespace TaskAgent.Api.Tests.Filters;

public class ValidationActionFilterTests
{
    [Fact]
    public async Task WhenNoValidatorRegistered_ShouldCallNext()
    {
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(sp => sp.GetService(It.IsAny<Type>())).Returns((object?)null);
        var filter = new ValidationActionFilter(serviceProvider.Object);

        var context = CreateContext(new CreateTaskRequest("Title", null, null, null, 1, 1, 1, null, null, null, null));
        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(CreateExecutedContext());
        };

        await filter.OnActionExecutionAsync(context, next);

        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task WhenValidationFails_ShouldReturnBadRequestWithFirstErrorMessage()
    {
        var validator = new Mock<IValidator<CreateTaskRequest>>();
        validator.Setup(v => v.Validate(It.IsAny<CreateTaskRequest>()))
            .Returns(new ValidationResult(new[] { new ValidationFailure("Title", "Title is required") }));

        var serviceProvider = new Mock<IServiceProvider>();
        var validatorType = typeof(IValidator<>).MakeGenericType(typeof(CreateTaskRequest));
        serviceProvider.Setup(sp => sp.GetService(validatorType)).Returns(validator.Object);

        var filter = new ValidationActionFilter(serviceProvider.Object);
        var req = new CreateTaskRequest("", null, null, null, 1, 1, 1, null, null, null, null);
        var context = CreateContext(req);
        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(CreateExecutedContext());
        };

        await filter.OnActionExecutionAsync(context, next);

        Assert.False(nextCalled);
        Assert.NotNull(context.Result);
        var badRequest = Assert.IsType<BadRequestObjectResult>(context.Result);
        var body = Assert.IsType<ApiErrorDto>(badRequest.Value);
        Assert.Equal("Title is required", body.Message);
    }

    [Fact]
    public async Task WhenValidationSucceeds_ShouldCallNext()
    {
        var validator = new Mock<IValidator<CreateTaskRequest>>();
        validator.Setup(v => v.Validate(It.IsAny<CreateTaskRequest>()))
            .Returns(new ValidationResult());

        var serviceProvider = new Mock<IServiceProvider>();
        var validatorType = typeof(IValidator<>).MakeGenericType(typeof(CreateTaskRequest));
        serviceProvider.Setup(sp => sp.GetService(validatorType)).Returns(validator.Object);

        var filter = new ValidationActionFilter(serviceProvider.Object);
        var req = new CreateTaskRequest("Valid", null, null, null, 1, 1, 1, null, null, null, null);
        var context = CreateContext(req);
        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(CreateExecutedContext());
        };

        await filter.OnActionExecutionAsync(context, next);

        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }

    private static ActionExecutingContext CreateContext(object? argument)
    {
        var actionArgs = new Dictionary<string, object?>();
        if (argument != null)
            actionArgs["req"] = argument;
        var context = new ActionExecutingContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(),
            actionArgs,
            null!);
        return context;
    }

    private static ActionExecutedContext CreateExecutedContext()
    {
        return new ActionExecutedContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(),
            null!);
    }
}
