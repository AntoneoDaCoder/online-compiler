using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Contracts.DTOs;
using Microsoft.Extensions.Options;
using ServerAPIApp.Core.Configs;
using ServerAPIApp.Domain.Exceptions.BadRequestExceptions;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ServerAPIApp.Core.Services
{
    public class KeycloakService : IKeycloakService
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

        public async Task<KeycloakTokenResponseDto> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            var formData = new Dictionary<string, string>
            {
                ["client_id"] = _configuration.ClientId,
                ["client_secret"] = _configuration.ClientSecret,
                ["grant_type"] = "password",
                ["username"] = email,
                ["password"] = password,
                ["scope"] = "openid profile email"
            };

            using var content = new FormUrlEncodedContent(formData);

            var response = await _httpClient.PostAsync(
                $"realms/{_configuration.Realm}/protocol/openid-connect/token",
                content,
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            response.EnsureSuccessStatusCode();

            return JsonSerializer.Deserialize<KeycloakTokenResponseDto>(body, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize token response");
        }

        public async Task<string> RegisterUserAsync(string email, string password, IEnumerable<string> roles, CancellationToken cancellationToken = default)
        {
            var adminToken = await GetAdminTokenAsync(cancellationToken);

            var userData = new
            {
                username = email,
                email,
                enabled = true,
                emailVerified = false,
                credentials = new[]
                {
                    new { type = "password", value = password, temporary = false }
                }
            };

            var jsonContent = JsonSerializer.Serialize(userData);
            using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var requestMessage = new HttpRequestMessage(HttpMethod.Post,
                $"admin/realms/{_configuration.Realm}/users")
            {
                Content = content
            };

            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            response.EnsureSuccessStatusCode();

            if (response.Headers.Location != null)
            {
                var uri = response.Headers.Location;
                var userId = uri.Segments.Last().TrimEnd('/');

                await SendVerifyEmailAsync(userId, adminToken, cancellationToken);

                await AssignRealmRolesAsync(userId, adminToken, roles, cancellationToken);

                return userId;
            }
            else
                throw new HttpRequestException("User id is missing. Cannot verify email", null, System.Net.HttpStatusCode.Unauthorized);
        }

        public async Task<KeycloakTokenResponseDto> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new EmptyFieldException("Refresh token is required.");

            var formData = new Dictionary<string, string>
            {
                ["client_id"] = _configuration.ClientId,
                ["client_secret"] = _configuration.ClientSecret,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
            };

            using var content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.PostAsync(
                $"realms/{_configuration.Realm}/protocol/openid-connect/token",
                content,
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            response.EnsureSuccessStatusCode();

            var tokenResp = JsonSerializer.Deserialize<KeycloakTokenResponseDto>(body, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize token response");

            return tokenResp;
        }

        public async Task LogoutAsync(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
                return;

            var formData = new Dictionary<string, string>
            {
                ["client_id"] = _configuration.ClientId,
                ["client_secret"] = _configuration.ClientSecret,
                ["token"] = token,
                ["token_type_hint"] = "refresh_token"
            };

            using var content = new FormUrlEncodedContent(formData);

            var response = await _httpClient.PostAsync(
                $"realms/{_configuration.Realm}/protocol/openid-connect/revoke",
                content,
                cancellationToken);

            response.EnsureSuccessStatusCode();
        }

        public async Task<KeycloakUserResponseDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
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

            var users = JsonSerializer.Deserialize<List<KeycloakUserResponseDto>>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize user response");

            return users.FirstOrDefault();
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

            var user = JsonSerializer.Deserialize<KeycloakUserResponseDto>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize user response");

            return user.Email;
        }

        private async Task AssignRealmRolesAsync(string userId, string adminToken, IEnumerable<string> roleNames, CancellationToken cancellationToken)
        {
            var urlRoles = $"admin/realms/{_configuration.Realm}/roles";
            var request = new HttpRequestMessage(HttpMethod.Get, urlRoles);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var respAll = await _httpClient.SendAsync(request, cancellationToken);
            respAll.EnsureSuccessStatusCode();

            var allRoles = JsonSerializer.Deserialize<IEnumerable<KeycloakRoleDto>>(await respAll.Content.ReadAsStringAsync(cancellationToken), _jsonOptions);

            List<KeycloakRoleDto> rolesToAssign = new List<KeycloakRoleDto>();

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

        private async Task SendVerifyEmailAsync
            (string userId,
            string adminToken,
            CancellationToken cancellationToken = default)
        {
            var url = $"admin/realms/{_configuration.Realm}/users/{userId}/execute-actions-email";

            var query = new List<string>();

            query.Add($"client_id={Uri.EscapeDataString(_configuration.ClientId)}");
            query.Add($"redirect_uri={Uri.EscapeDataString(_configuration.RedirectUrl)}");
            query.Add($"lifespan={_configuration.EmailConfirmationLifetimeSeconds}");

            url += "?" + string.Join("&", query);

            var actions = new List<string>() { "VERIFY_EMAIL" };

            var jsonContent = JsonSerializer.Serialize(actions);
            using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Put, url);
            request.Content = content;
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var resp = await _httpClient.SendAsync(request, cancellationToken);

            resp.EnsureSuccessStatusCode();
        }

        private async Task<string> GetAdminTokenAsync(CancellationToken cancellationToken = default)
        {
            var formData = new Dictionary<string, string>
            {
                ["client_id"] = "admin-cli",
                ["grant_type"] = "password",
                ["username"] = _configuration.AdminEmail,
                ["password"] = _configuration.AdminPassword
            };

            var content = new FormUrlEncodedContent(formData);

            var response = await _httpClient.PostAsync(
                $"realms/master/protocol/openid-connect/token",
                content,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            var tokenResponse = JsonSerializer.Deserialize<KeycloakTokenResponseDto>(json);

            return tokenResponse?.AccessToken
                ?? throw new InvalidOperationException("Failed to get admin token");
        }
    }
}
