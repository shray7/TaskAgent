using FluentValidation;
using TaskAgent.Contracts.Requests;

namespace TaskAgent.Api.Validators;

public class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
    public CreateCommentRequestValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.AuthorId).GreaterThan(0);
    }
}
