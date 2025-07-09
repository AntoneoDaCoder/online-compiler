using Shared.Enums;

namespace Shared.DTOs
{
    public sealed class CodeResponseDto
    {
        public Guid RequestId { get; set; }
        public RequestStatus Status { get; set; } = default;
        public ExecutionResultDto Result { get; set; }
    }
}
