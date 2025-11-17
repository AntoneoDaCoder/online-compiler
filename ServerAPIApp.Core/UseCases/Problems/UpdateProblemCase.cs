using MediatR;

namespace ServerAPIApp.Core.UseCases.Problems
{
    public record UpdateProblemCase(Guid ProblemId, string Title, string Slug) : IRequest;

}
