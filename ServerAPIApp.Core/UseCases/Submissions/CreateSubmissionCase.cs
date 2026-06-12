using MediatR;
using ServerAPIApp.Domain.Entities;
using Shared.DTOs;

namespace ServerAPIApp.Core.UseCases.Submissions
{
    public record CreateSubmissionCase(CodeResponseDto Response) : IRequest<SubmissionEntity>;
}
