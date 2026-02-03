using FluentValidation;
using TaskAgent.Contracts.Requests;

namespace TaskAgent.Api.Validators;

public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    private static readonly string[] AllowedStatuses = ["todo", "in-progress", "completed"];
    private static readonly string[] AllowedPriorities = ["low", "medium", "high"];

    public UpdateTaskRequestValidator()
    {
        When(x => x.Title != null, () => RuleFor(x => x.Title!).NotEmpty().MaximumLength(500));
        When(x => x.Status != null, () => RuleFor(x => x.Status!).Must(s => AllowedStatuses.Contains(s)).WithMessage("Status must be one of: todo, in-progress, completed"));
        When(x => x.Priority != null, () => RuleFor(x => x.Priority!).Must(p => AllowedPriorities.Contains(p)).WithMessage("Priority must be one of: low, medium, high"));
        When(x => x.Size.HasValue, () => RuleFor(x => x.Size!.Value).GreaterThanOrEqualTo(0));
    }
}
