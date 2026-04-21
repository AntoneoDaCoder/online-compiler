namespace ServerAPIApp.Domain.Entities
{
    public class ProblemDeletionRequestEntity : IBaseEntity
    {
        public Guid Id { get; set; }

        public Guid ProblemId { get; set; }
        public ProblemEntity Problem { get; set; }

        public Guid InitiatorId { get; set; }
        public UserEntity Initiator { get; set; }

        public bool IsApproved { get; set; }

        public string Reason { get; set; } = string.Empty;
    }
}
