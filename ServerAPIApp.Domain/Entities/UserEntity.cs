using Microsoft.AspNetCore.Identity;

namespace ServerAPIApp.Domain.Entities
{
    public sealed class UserEntity : IdentityUser<Guid>, ICreatable, IModifiable, ISoftDeletable
    {
        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public UserEntity? Creator { get; set; }

        public DateTimeOffset ModifiedAt { get; set; }
        public Guid ModifiedBy { get; set; }
        public UserEntity? Editor { get; set; }

        public string? Name { get; set; }
        public string? EncryptedEmail { get; set; }   // IDataProtector protected string
        public string? EmailHash { get; set; }


        public string? RefreshToken { get; set; } //gonna be encrypted in db
        public DateTimeOffset? RefreshTokenExpiryTime { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTimeOffset? DeletionScheduledAt { get; set; }
        public DateTimeOffset? DeletionDeadline { get; set; }
        public Guid? InitiatorId { get; set; }
        public UserEntity? Initiator { get; set; }
    }
}
