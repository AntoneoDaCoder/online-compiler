using MediatR;
using ServerAPIApp.Contracts.DTOs;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Core.UseCases.Problems
{
    public record CreateProblemCase(Guid CreatorId, string Title, string Slug) : IRequest<ProblemDto>;
}
