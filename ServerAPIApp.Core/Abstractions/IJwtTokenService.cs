namespace ServerAPIApp.Core.Abstractions
{
    public interface IJwtTokenService
    {
        Task<Guid> GetUserIdFromTokenAsync(string accessToken, CancellationToken cancellationToken = default);
        string CreateNewRefreshToken();
        DateTimeOffset GetTokenExpirationTime(DateTimeOffset dateIssued);
        string GenerateAccessToken(IEnumerable<string> roles, Guid userId);
    }
}
