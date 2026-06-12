using MediatR;
using ServerAPIApp.Contracts.DTOs;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Core.UseCases.Submissions
{
    public record GetSubmissionByIdCase(Guid Id) : IRequest<SubmissionDto>;
}
