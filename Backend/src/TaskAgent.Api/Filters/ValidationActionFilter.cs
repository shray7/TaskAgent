using System.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TaskAgent.Contracts.Dtos;

namespace TaskAgent.Api.Filters;

/// <summary>
/// Runs FluentValidation for any action argument that has a registered validator.
/// On failure returns 400 Bad Request with <see cref="ApiErrorDto"/> containing the first error message.
/// </summary>
public sealed class ValidationActionFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationActionFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var value in context.ActionArguments.Values)
        {
            if (value == null) continue;

            var modelType = value.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(modelType);
            var validator = _serviceProvider.GetService(validatorType);
            if (validator == null) continue;

            var validateMethod = validatorType.GetMethod(nameof(IValidator<object>.Validate), [modelType]);
            if (validateMethod?.Invoke(validator, [value]) is not FluentValidation.Results.ValidationResult result)
                continue;

            if (result.IsValid) continue;

            var firstMessage = result.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed.";
            context.Result = new BadRequestObjectResult(new ApiErrorDto(firstMessage));
            return;
        }

        await next();
    }
}
