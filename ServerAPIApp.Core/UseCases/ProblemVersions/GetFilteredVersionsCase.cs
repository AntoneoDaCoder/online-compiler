using MediatR;
using ServerAPIApp.Contracts.DTOs.Problems;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Core.UseCases.ProblemVersions
{
    public record GetFilteredVersionsCase() : IRequest<IEnumerable<EditorProblemVersionDto>?>;
}
