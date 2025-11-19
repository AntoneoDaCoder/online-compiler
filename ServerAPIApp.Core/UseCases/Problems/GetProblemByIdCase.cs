using MediatR;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Core.UseCases.Problems
{
    public record GetProblemByIdCase(Guid Id) : IRequest<ProblemEntity>;
}
