using Shared.Enums;

namespace Shared.DTOs
{
    public sealed class CodeResponseDto
    {
        public Guid RequestId { get; set; }
        public RequestStatus Status { get; set; } = default;
        public string Language { get; set; } = string.Empty;
        public ExecutionResultDto Result { get; set; }
    }
}
