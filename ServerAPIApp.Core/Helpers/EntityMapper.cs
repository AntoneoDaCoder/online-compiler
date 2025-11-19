using ServerAPIApp.Core.UseCases.Languages;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Core.Helpers
{
    public static class EntityMapper
    {
        #region LanguageEntity related mapping
        public static LanguageEntity ToEntity(this CreateLanguageCase command)
        {
            var entity = new LanguageEntity()
            {
                Id = Guid.NewGuid(),
                DisplayName = command.DisplayName,
                Code = command.Code,
            };

            return entity;
        }

        public static LanguageEntity ToEntity(this UpdateLanguageCase command)
        {
            var entity = new LanguageEntity()
            {
                Id = command.Id,
                DisplayName = command.DisplayName,
                Code = command.Code,
            };

            return entity;
        }
        #endregion
    }
}
