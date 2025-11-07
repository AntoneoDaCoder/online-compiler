namespace ServerAPIApp.Domain.Entities
{
    public abstract class BaseEntity : IBaseEntity
    {
        public Guid Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ModifiedAt { get; set; }
    }
}
