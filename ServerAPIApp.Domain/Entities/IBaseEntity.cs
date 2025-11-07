namespace ServerAPIApp.Domain.Entities
{
    public interface IBaseEntity : ICreatable, IModifiable
    {
        Guid Id { get; set; }
    }
}
