using MediatR;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Core.UseCases.Submissions
{
    public record GetSubmissionByIdCase(Guid Id) : IRequest<SubmissionEntity>;
}
