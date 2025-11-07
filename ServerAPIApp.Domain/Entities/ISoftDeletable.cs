namespace ServerAPIApp.Domain.Entities
{
    public interface ISoftDeletable
    {
        bool IsDeleted { get; set; }
    }
}
