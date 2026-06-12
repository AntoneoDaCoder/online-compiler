using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Contracts.DTOs
{
    public record LanguageDto(Guid Id, string Code, string DisplayName)
    {
        public static LanguageDto From(Guid id, string code, string name)
        {
            return new LanguageDto(id, code, name);
        }

        public static LanguageDto From(LanguageEntity entity)
        {
            return new LanguageDto(entity.Id, entity.Code, entity.DisplayName);
        }
    }
}
