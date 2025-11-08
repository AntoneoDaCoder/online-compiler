namespace ServerAPIApp.Domain.Entities
{
    public interface IPublishable
    {
        bool IsPublished { get; set; }
        Guid? PublishedBy { get; set; }
        UserEntity? Publisher { get; set; }
    }
}
