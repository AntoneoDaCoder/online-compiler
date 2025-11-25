using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Contracts.DTOs
{
    public record DraftListDto(List<ShortDraftDto>? Drafts);
}
