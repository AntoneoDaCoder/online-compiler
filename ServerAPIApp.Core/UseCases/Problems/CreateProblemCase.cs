using MediatR;

namespace ServerAPIApp.Core.UseCases.Problems
{
    public record CreateProblemCase(string Title, string Slug) : IRequest;
}
