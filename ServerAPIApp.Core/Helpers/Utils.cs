using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace ServerAPIApp.Core.Helpers
{
    public static class Utils
    {
        public static (string ExternalUserId, string[] RealmRoles) ReadToken(string accessToken)
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

            var payload = JsonDocument.Parse(jwt.Payload.SerializeToJson()).RootElement;

            var userId = jwt.Subject
                ?? throw new InvalidOperationException("Keycloak token does not contain subject.");

            var realmRoles = Array.Empty<string>();

            if (payload.TryGetProperty("realm_access", out var realmAccess) &&
                realmAccess.TryGetProperty("roles", out var rolesJson))
            {
                realmRoles = rolesJson.EnumerateArray()
                    .Select(x => x.GetString())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray()!;
            }

            return (userId, realmRoles);
        }
    }
}
