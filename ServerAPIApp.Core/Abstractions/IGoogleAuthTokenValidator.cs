using ServerAPIApp.Contracts.DTOs;

namespace ServerAPIApp.Core.Abstractions
{
    public interface IGoogleAuthTokenValidator
    {
        Task<GoogleAuthPayload?> ValidateTokenAsync
            (string idToken, 
            CancellationToken cancellationToken = default);
    }
}
