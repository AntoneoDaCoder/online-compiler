using MediatR;

namespace ServerAPIApp.Core.UseCases.Problems
{
    public record GetProblemByIdCase(Guid Id) : IRequest;
}
