namespace ServerAPIApp.Domain.Entities
{
    public interface IModifiable
    {
        DateTimeOffset? ModifiedAt { get; set; }
        Guid? ModifiedBy { get; set; }
        UserEntity? Editor { get; set; }
    }
}
