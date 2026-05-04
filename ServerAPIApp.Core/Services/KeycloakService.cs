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

            var request = new HttpRequestMessage(
                HttpMethod.Get,
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

        public async Task<IEnumerable<ExternalUserResponseDto>?> GetAllUsersAsync(CancellationToken cancellationToken = default)
        {
            var adminToken = await GetAdminTokenAsync(cancellationToken);

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"admin/realms/{_configuration.Realm}/users");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrEmpty(json) || json == "null" || json == "[]")
                return null;

            var users = JsonSerializer.Deserialize<IEnumerable<ExternalUserResponseDto>>(json, _jsonOptions)
                        ?? throw new InvalidOperationException("Failed to deserialize user response");

            return users;
        }

        public async Task<IEnumerable<ExternalRoleDto>?> GetUserClientRolesAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new EmptyFieldException("Keycloak account id is required.");

            var adminToken = await GetAdminTokenAsync(cancellationToken);
            var clientUuid = await GetClientUuidAsync(adminToken, cancellationToken);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"admin/realms/{_configuration.Realm}/users/{Uri.EscapeDataString(userId)}/role-mappings/clients/{clientUuid}");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(json) || json == "null" || json == "[]")
                return null;

            var roles = JsonSerializer.Deserialize<IEnumerable<ExternalRoleDto>>(json, _jsonOptions)
                        ?? throw new InvalidOperationException("Failed to deserialize user client roles response");

            return roles;
        }

        public async Task<Guid> CreateUserWithRolesAsync(
            string email,
            string username,
            string password,
            IEnumerable<string> roles,
            CancellationToken cancellationToken = default)
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
                throw new InvalidOperationException(
                    $"Failed to create user. Status: {(int)createResponse.StatusCode}. Body: {error}");
            }

            var userId = createResponse.Headers.Location?.Segments.LastOrDefault()?.Trim('/');

            if (userId is null)
                throw new InvalidOperationException(
                    $"Failed to create user. Status: {(int)createResponse.StatusCode}. User id not found");

            await SetPasswordAsync(userId, password, adminToken, cancellationToken);

            if (roles.Any())
            {
                var allRoles = await GetClientRoleDataAsync(adminToken, cancellationToken);
                await AssignClientRolesAsync(userId, adminToken, allRoles!, roles, cancellationToken);
            }

            return Guid.Parse(userId);
        }

        public async Task DeleteAccountAsync(string keycloakAccountId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(keycloakAccountId))
                throw new EmptyFieldException("Keycloak account id is required.");

            var adminToken = await GetAdminTokenAsync(cancellationToken);

            using var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"admin/realms/{_configuration.Realm}/users/{Uri.EscapeDataString(keycloakAccountId)}");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return;

            response.EnsureSuccessStatusCode();
        }

        public async Task<string?> GetUserEmailByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new EmptyFieldException("Keycloak account id is required.");

            var adminToken = await GetAdminTokenAsync(cancellationToken);

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"admin/realms/{_configuration.Realm}/users/{Uri.EscapeDataString(userId)}");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            var user = JsonSerializer.Deserialize<ExternalUserResponseDto>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize user response");

            return user.Email;
        }

        public async Task<IEnumerable<ExternalRoleDto>?> GetAvailableClientRolesAsync(CancellationToken cancellationToken = default)
        {
            var adminToken = await GetAdminTokenAsync(cancellationToken);
            return await GetClientRoleDataAsync(adminToken, cancellationToken);
        }

        public async Task UpdateUserClientRolesAsync(
            string userId,
            IEnumerable<string> rolesToRemove,
            IEnumerable<string> rolesToAdd,
            CancellationToken cancellationToken = default)
        {
            var adminToken = await GetAdminTokenAsync(cancellationToken);
            var allRoles = await GetClientRoleDataAsync(adminToken, cancellationToken);

            if (rolesToRemove.Any())
            {
                await UnassignClientRolesAsync(userId, adminToken, allRoles!, rolesToRemove, cancellationToken);
            }

            if (rolesToAdd.Any())
            {
                await AssignClientRolesAsync(userId, adminToken, allRoles!, rolesToAdd, cancellationToken);
            }
        }

        private async Task<string> GetClientUuidAsync(string adminToken, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"admin/realms/{_configuration.Realm}/clients?clientId={Uri.EscapeDataString(_configuration.FrontEndClientId)}");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            var clients = JsonSerializer.Deserialize<List<ClientRepresentationDto>>(json, _jsonOptions)
                         ?? throw new InvalidOperationException("Failed to deserialize clients response");

            var client = clients.FirstOrDefault();
            if (client?.Id is null)
                throw new InvalidOperationException($"Client '{_configuration.FrontEndClientId}' not found");

            return client.Id;
        }

        private async Task<IEnumerable<ExternalRoleDto>?> GetClientRoleDataAsync(
            string adminToken,
            CancellationToken cancellationToken = default)
        {
            var clientUuid = await GetClientUuidAsync(adminToken, cancellationToken);

            var urlRoles = $"admin/realms/{_configuration.Realm}/clients/{clientUuid}/roles";
            var request = new HttpRequestMessage(HttpMethod.Get, urlRoles);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var respAll = await _httpClient.SendAsync(request, cancellationToken);
            respAll.EnsureSuccessStatusCode();

            var allRoles = JsonSerializer.Deserialize<IEnumerable<ExternalRoleDto>>(
                await respAll.Content.ReadAsStringAsync(cancellationToken),
                _jsonOptions);

            return allRoles;
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

        private async Task AssignClientRolesAsync(
            string userId,
            string adminToken,
            IEnumerable<ExternalRoleDto> allRoles,
            IEnumerable<string> roles,
            CancellationToken cancellationToken)
        {
            var roleNamesSet = roles.ToHashSet(StringComparer.Ordinal);

            var rolesToAssign = allRoles
                .Where(role => roleNamesSet.Contains(role.RoleName))
                .ToList();

            if (rolesToAssign.Count == 0)
                return;

            var clientUuid = await GetClientUuidAsync(adminToken, cancellationToken);
            var assignUrl = $"admin/realms/{_configuration.Realm}/users/{userId}/role-mappings/clients/{clientUuid}";

            using var body = new StringContent(
                JsonSerializer.Serialize(rolesToAssign),
                Encoding.UTF8,
                "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, assignUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            request.Content = body;

            var respAssign = await _httpClient.SendAsync(request, cancellationToken);
            respAssign.EnsureSuccessStatusCode();
        }

        private async Task UnassignClientRolesAsync(
            string userId,
            string adminToken,
            IEnumerable<ExternalRoleDto> allRoles,
            IEnumerable<string> roles,
            CancellationToken cancellationToken)
        {
            var roleNamesSet = roles.ToHashSet(StringComparer.Ordinal);

            var rolesToRemove = allRoles
                .Where(role => roleNamesSet.Contains(role.RoleName))
                .ToList();

            if (rolesToRemove.Count == 0)
                return;

            var clientUuid = await GetClientUuidAsync(adminToken, cancellationToken);
            var removeUrl = $"admin/realms/{_configuration.Realm}/users/{userId}/role-mappings/clients/{clientUuid}";

            using var body = new StringContent(
                JsonSerializer.Serialize(rolesToRemove),
                Encoding.UTF8,
                "application/json");

            var request = new HttpRequestMessage(HttpMethod.Delete, removeUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            request.Content = body;

            var respRemove = await _httpClient.SendAsync(request, cancellationToken);
            respRemove.EnsureSuccessStatusCode();
        }
    }

    public sealed class ClientRepresentationDto
    {
        public string? Id { get; set; }
        public string? ClientId { get; set; }
    }
}