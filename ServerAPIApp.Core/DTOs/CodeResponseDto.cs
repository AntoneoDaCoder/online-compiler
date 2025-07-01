using ServerAPIApp.Core.Enums;

namespace ServerAPIApp.Core.DTOs
{
    public sealed class CodeResponseDto
    {
        public Guid RequestId { get; set; }
        public RequestStatus Status { get; set; } = default;
        public ExecutionResultDto Result { get; set; }
    }
}
