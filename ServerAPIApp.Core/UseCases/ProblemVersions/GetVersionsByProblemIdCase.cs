using MediatR;

namespace ServerAPIApp.Core.UseCases.ProblemVersions
{
    public record GetVersionsByProblemIdCase(Guid ProblemId) : IRequest;
}
