namespace ServerAPIApp.Domain.Entities
{
    public interface ICreatable
    {
        DateTimeOffset CreatedAt { get; set; }
    }
}
