using MediatR;

namespace ServerAPIApp.Core.UseCases.Submissions
{
    public record GetSubmissionByIdCase(Guid Id) : IRequest;
}
