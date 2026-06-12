using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;

namespace ServerAPIApp.Core.Services
{
    public class KeycloakClaimTransformer : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (principal.Identity is not ClaimsIdentity identity)
                return Task.FromResult(principal);

            var existingRoles = new HashSet<string>(
                identity.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value),
                StringComparer.OrdinalIgnoreCase);

            void AddRole(string role)
            {
                if (string.IsNullOrWhiteSpace(role)) return;

                if (existingRoles.Add(role))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
                }
            }

            void AddClaimIfMissing(string type, string value)
            {
                if (string.IsNullOrWhiteSpace(value)) return;

                if (!identity.HasClaim(c => c.Type == type))
                {
                    identity.AddClaim(new Claim(type, value));
                }
            }

            var sub = identity.FindFirst("sub")?.Value;

            AddClaimIfMissing(ClaimTypes.NameIdentifier, sub);

            var resourceAccess = identity.FindFirst("resource_access")?.Value;
            if (!string.IsNullOrEmpty(resourceAccess))
            {
                try
                {
                    using var doc = JsonDocument.Parse(resourceAccess);
                    foreach (var clientProp in doc.RootElement.EnumerateObject())
                    {
                        var clientObj = clientProp.Value;
                        if (clientObj.ValueKind == JsonValueKind.Object &&
                            clientObj.TryGetProperty("roles", out var rolesElement) &&
                            rolesElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var el in rolesElement.EnumerateArray())
                            {
                                if (el.ValueKind == JsonValueKind.String)
                                    AddRole(el.GetString()!);
                            }
                        }
                    }
                }
                catch (JsonException) { }
            }

            return Task.FromResult(principal);
        }
    }
}