using FluentValidation;
using ServerAPIApp.Core.UseCases.Problems;

namespace ServerAPIApp.Core.Validators.Problems
{
    public class CreateProblemCaseValidator : AbstractValidator<CreateProblemCase>
    {
        public CreateProblemCaseValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("'Title' cannot be empty");

            RuleFor(x => x.Slug)
                .NotEmpty()
                .WithMessage("'Slug' cannot be empty");
        }
    }
}
