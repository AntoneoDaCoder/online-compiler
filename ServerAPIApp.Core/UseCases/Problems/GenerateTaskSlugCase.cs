using MediatR;

namespace ServerAPIApp.Core.UseCases.Problems
{
    public record GenerateTaskSlugCase() : IRequest<string>;
}
