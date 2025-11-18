using MediatR;

namespace ServerAPIApp.Core.UseCases.Submissions
{
    public record DeleteSubmissionCase(Guid Id) : IRequest;
}
