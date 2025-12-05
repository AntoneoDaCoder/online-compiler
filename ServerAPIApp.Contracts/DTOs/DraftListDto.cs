using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Contracts.DTOs
{
    public record DraftListDto(List<ShortDraftDto>? Drafts)
    {
        public static DraftListDto From(List<ProblemVersionEntity>? data)
        {
            if (data is null)
                return new DraftListDto(Drafts: null);

            var entities = data
                .Select
                (
                    x => new ShortDraftDto(x.Id, x.ProblemId, x.Creator.Name, x.CreatedAt)
                )
                .ToList();

            return new DraftListDto(entities);
        }
    }
}
