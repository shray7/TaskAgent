using FluentValidation;
using TaskAgent.Contracts.Dtos;

namespace TaskAgent.Api.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(128);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).WithMessage("Password must be at least 8 characters").MaximumLength(100);
    }
}
