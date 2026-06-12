using Microsoft.AspNetCore.SignalR;
using ServerAPIApp.Extensions;

namespace ServerAPIApp.IdProviders
{
    public class JwtUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            var user = connection.User;

            if (user == null) return null;

            var idClaim = user.GetStringUserId();

            return idClaim;
        }
    }
}
