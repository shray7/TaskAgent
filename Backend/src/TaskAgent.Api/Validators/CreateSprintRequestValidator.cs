using FluentValidation;
using TaskAgent.Contracts.Requests;

namespace TaskAgent.Api.Validators;

public class CreateSprintRequestValidator : AbstractValidator<CreateSprintRequest>
{
    public CreateSprintRequestValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.StartDate).NotEmpty().WithMessage("Start date is required");
    }
}
