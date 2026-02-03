using FluentValidation;
using TaskAgent.Contracts.Requests;

namespace TaskAgent.Api.Validators;

public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    private static readonly int[] AllowedSprintDurations = [7, 14, 21, 28];
    private static readonly string[] AllowedTaskSizeUnits = ["hours", "days"];

    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.OwnerId).GreaterThan(0);
        When(x => x.SprintDurationDays.HasValue, () => RuleFor(x => x.SprintDurationDays!.Value).Must(d => AllowedSprintDurations.Contains(d)).WithMessage("Sprint duration must be 7, 14, 21, or 28 days"));
        When(x => !string.IsNullOrEmpty(x.TaskSizeUnit), () => RuleFor(x => x.TaskSizeUnit!).Must(u => AllowedTaskSizeUnits.Contains(u)).WithMessage("Task size unit must be hours or days"));
    }
}
