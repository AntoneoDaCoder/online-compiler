namespace ServerAPIApp.Domain.Entities
{
    public interface IModifiable
    {
        DateTimeOffset ModifiedAt { get; set; }
    }
}
