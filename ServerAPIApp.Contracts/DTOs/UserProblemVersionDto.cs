using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Contracts.DTOs
{
    public record UserProblemVersionDto(Guid ProblemId, string Statement, int TotalTests);
}
