namespace ServerAPIApp.Domain.Entities
{
    public interface ICreatable
    {
        DateTimeOffset? CreatedAt { get; set; }
        Guid? CreatedBy { get; set; }
        UserEntity? Creator { get; set; }
    }
}
