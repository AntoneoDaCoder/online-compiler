using MediatR;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Core.UseCases.Problems
{
    public record UpdateProblemCase(Guid ProblemId, Guid EditorId, string Title, string Slug) : IRequest<ProblemEntity>;

}
