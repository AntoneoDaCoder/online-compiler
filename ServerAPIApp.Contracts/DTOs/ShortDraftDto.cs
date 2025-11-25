using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Contracts.DTOs
{
    public record ShortDraftDto(Guid VersionId, Guid ProblemId, string CreatedBy, DateTimeOffset CreatedAt);
}
