using Shared.Enums;

namespace Shared.DTOs
{
    public sealed class CodeResponseDto
    {
        public Guid RequestId { get; set; }
        public RequestStatus Status { get; set; } = default;
        public Guid VersionId { get; set; }
        public Guid UserId { get; set; }
        public string UserSolution { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public ExecutionResultDto Result { get; set; }
    }
}
