using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;

namespace ServerAPIApp.Core.Services
{
    public class KeycloakClaimTransformer : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var identity = principal.Identity as ClaimsIdentity;
            if (identity == null) return Task.FromResult(principal);

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

            var realmAccess = identity.FindFirst("realm_access")?.Value;
            if (!string.IsNullOrEmpty(realmAccess))
            {
                try
                {
                    using var doc = JsonDocument.Parse(realmAccess);
                    if (doc.RootElement.TryGetProperty("roles", out var rolesElement) && rolesElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var el in rolesElement.EnumerateArray())
                        {
                            if (el.ValueKind == JsonValueKind.String)
                                AddRole(el.GetString()!);
                        }
                    }
                }
                catch (JsonException) { /* ignore malformed claim */ }
            }

            var resourceAccess = identity.FindFirst("resource_access")?.Value;
            if (!string.IsNullOrEmpty(resourceAccess))
            {
                try
                {
                    using var doc = JsonDocument.Parse(resourceAccess);
                    foreach (var clientProp in doc.RootElement.EnumerateObject())
                    {
                        var clientObj = clientProp.Value;
                        if (clientObj.ValueKind == JsonValueKind.Object && clientObj.TryGetProperty("roles", out var rolesElement) && rolesElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var el in rolesElement.EnumerateArray())
                            {
                                if (el.ValueKind == JsonValueKind.String)
                                    AddRole(el.GetString()!);
                            }
                        }
                    }
                }
                catch (JsonException) { /* ignore malformed claim */ }
            }

            return Task.FromResult(principal);
        }
    }
}
