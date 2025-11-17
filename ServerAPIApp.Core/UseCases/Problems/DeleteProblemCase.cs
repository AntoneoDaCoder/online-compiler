using MediatR;

namespace ServerAPIApp.Core.UseCases.Problems
{
    public record DeleteProblemCase(Guid ProblemId) : IRequest;
}
