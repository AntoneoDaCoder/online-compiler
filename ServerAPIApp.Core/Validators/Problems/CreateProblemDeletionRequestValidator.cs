using FluentValidation;
using ServerAPIApp.Core.UseCases.Problems;
using ServerAPIApp.Domain.Constants;

namespace ServerAPIApp.Core.Validators.Problems
{
    public class CreateProblemDeletionRequestValidator : AbstractValidator<CreateProblemDeletionRequestCase>
    {
        public CreateProblemDeletionRequestValidator()
        {
            RuleFor(x => x.InitiatorId)
                .NotEmpty()
                .WithMessage("'InitiatorId' must be set");

            RuleFor(x => x.ProblemId)
                .NotEmpty()
                .WithMessage("'ProblemId' must be set");

            RuleFor(x => x.Reason)
                .NotEmpty()
                .Length(ApplicationConstants.DeletionRequestReasonMinLength, ApplicationConstants.DeletionRequestReasonMaxLength)
                .WithMessage($"Deletion reason is required. Min length is {ApplicationConstants.DeletionRequestReasonMinLength}, max - {ApplicationConstants.DeletionRequestReasonMaxLength}");
        }
    }
}
