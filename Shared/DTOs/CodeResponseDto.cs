using Shared.Enums;

namespace Shared.DTOs
{
    public sealed class CodeResponseDto
    {
        public required Guid RequestId { get; set; }
        public RequestStatus Status { get; set; } = default;
        public required Guid VersionId { get; set; }
        public required Guid UserId { get; set; }
        public required string UserSolution { get; set; } = string.Empty;
        public required string Language { get; set; } = string.Empty;
        public required ExecutionResultDto Result { get; set; }
    }
}
