using FluentValidation;
using TaskAgent.Contracts.Requests;

namespace TaskAgent.Api.Validators;

public class UpdateSprintRequestValidator : AbstractValidator<UpdateSprintRequest>
{
    private static readonly string[] AllowedStatuses = ["planning", "active", "completed"];

    public UpdateSprintRequestValidator()
    {
        When(x => x.Name != null, () => RuleFor(x => x.Name!).NotEmpty().MaximumLength(128));
        When(x => !string.IsNullOrEmpty(x.Status), () => RuleFor(x => x.Status!).Must(s => AllowedStatuses.Contains(s)).WithMessage("Status must be one of: planning, active, completed"));
    }
}
