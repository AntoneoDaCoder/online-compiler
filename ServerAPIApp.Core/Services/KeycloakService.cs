using Microsoft.Extensions.Options;
using ServerAPIApp.Contracts.DTOs.Auth;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Core.Configs;
using ServerAPIApp.Domain.Exceptions.BadRequestExceptions;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ServerAPIApp.Core.Services
{
    public class KeycloakService : IExternalAuthService
    {
        private readonly HttpClient _httpClient;

        private readonly JsonSerializerOptions _jsonOptions =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private readonly KeycloakConfiguration _configuration;

        public KeycloakService(HttpClient client, IOptions<KeycloakConfiguration> configuration)
        {
            _httpClient = client;
            _configuration = configuration.Value;
        }

        public async Task<ExternalUserResponseDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var adminToken = await GetAdminTokenAsync(cancellationToken);

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"admin/realms/{_configuration.Realm}/users?email={Uri.EscapeDataString(email)}&exact=true");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrEmpty(json) || json == "null" || json == "[]")
                return null;

            var users = JsonSerializer.Deserialize<List<ExternalUserResponseDto>>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize user response");

            return users.FirstOrDefault();
        }

        public async Task<Guid> CreateUserWithRolesAsync(string email, string username, string password,
            IEnumerable<string> roles, CancellationToken cancellationToken = default)
        {
            var adminToken = await GetAdminTokenAsync(cancellationToken);

            var createUserBody = new
            {
                username,
                email,
                enabled = true,
                emailVerified = false
            };

            using var createRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"admin/realms/{_configuration.Realm}/users");

            createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            createRequest.Content = new StringContent(
                JsonSerializer.Serialize(createUserBody),
                Encoding.UTF8,
                "application/json");

            var createResponse = await _httpClient.SendAsync(createRequest, cancellationToken);

            if (createResponse.StatusCode != HttpStatusCode.Created)
            {
                var error = await createResponse.Content.ReadAsStringAsync(cancellationToken);

                throw new InvalidOperationException($"Failed to create user. Status: {(int)createResponse.StatusCode}. Body: {error}");
            }

            var userId = createResponse.Headers.Location?.Segments.LastOrDefault()?.Trim('/');

            if (userId is null)
                throw new InvalidOperationException($"Failed to create user. Status: {(int)createResponse.StatusCode}. User id not found");

            await SetPasswordAsync(userId, password, adminToken, cancellationToken);

            if (roles.Any())
                await AssignRealmRolesAsync(userId, roles, cancellationToken);

            return Guid.Parse(userId);
        }

        public async Task DeleteAccountAsync(string keycloakAccountId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(keycloakAccountId))
                throw new EmptyFieldException("Keycloak account id is required.");

            var adminToken = await GetAdminTokenAsync(cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Delete,
                $"admin/realms/{_configuration.Realm}/users/{Uri.EscapeDataString(keycloakAccountId)}");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return;

            response.EnsureSuccessStatusCode();
        }

        public async Task<string?> GetUserEmailByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new EmptyFieldException("Keycloak account id is required.");

            var adminToken = await GetAdminTokenAsync(cancellationToken);

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"admin/realms/{_configuration.Realm}/users/{Uri.EscapeDataString(userId)}");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            var user = JsonSerializer.Deserialize<ExternalUserResponseDto>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize user response");

            return user.Email;
        }

        public async Task AssignRealmRolesAsync(string userId, IEnumerable<string> roleNames, CancellationToken cancellationToken)
        {
            var adminToken = await GetAdminTokenAsync(cancellationToken);

            var urlRoles = $"admin/realms/{_configuration.Realm}/roles";
            var request = new HttpRequestMessage(HttpMethod.Get, urlRoles);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var respAll = await _httpClient.SendAsync(request, cancellationToken);
            respAll.EnsureSuccessStatusCode();

            var allRoles = JsonSerializer.Deserialize<IEnumerable<ExternalRoleDto>>(await respAll.Content.ReadAsStringAsync(cancellationToken), _jsonOptions);

            List<ExternalRoleDto> rolesToAssign = new List<ExternalRoleDto>();

            foreach (var role in allRoles)
            {
                if (roleNames.Contains(role.RoleName))
                {
                    rolesToAssign.Add(role);
                }
            }

            var assignUrl = $"admin/realms/{_configuration.Realm}/users/{userId}/role-mappings/realm";
            using var body = new StringContent(JsonSerializer.Serialize(rolesToAssign), Encoding.UTF8, "application/json");

            request = new HttpRequestMessage(HttpMethod.Post, assignUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            request.Content = body;

            var respAssign = await _httpClient.SendAsync(request, cancellationToken);
            respAssign.EnsureSuccessStatusCode();
        }

        private async Task<string> GetAdminTokenAsync(CancellationToken cancellationToken = default)
        {
            var formData = new Dictionary<string, string>
            {
                ["client_id"] = _configuration.ApiClientId,
                ["grant_type"] = "client_credentials",
                ["client_secret"] = _configuration.ApiClientSecret,
            };

            var content = new FormUrlEncodedContent(formData);

            var response = await _httpClient.PostAsync(
                $"realms/{_configuration.Realm}/protocol/openid-connect/token",
                content,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            var tokenResponse = JsonSerializer.Deserialize<ExternalTokenResponseDto>(json);

            return tokenResponse?.AccessToken
                ?? throw new InvalidOperationException("Failed to get admin token");
        }

        private async Task SetPasswordAsync(
            string userId,
            string password,
            string adminToken,
            CancellationToken cancellationToken = default)
        {
            var passwordBody = new
            {
                type = "password",
                value = password,
                temporary = false
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"admin/realms/{_configuration.Realm}/users/{Uri.EscapeDataString(userId)}/reset-password");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            request.Content = new StringContent(
                JsonSerializer.Serialize(passwordBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            response.EnsureSuccessStatusCode();
        }
    }
}
