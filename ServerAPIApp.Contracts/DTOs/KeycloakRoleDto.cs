using System.Text.Json.Serialization;

namespace ServerAPIApp.Contracts.DTOs
{
    public record KeycloakRoleDto
    {
        [JsonPropertyName("id")]
        public string Id { get; } = string.Empty;
        [JsonPropertyName("name")]
        public string RoleName { get; } = string.Empty;

        public KeycloakRoleDto(string id, string roleName)
        {
            Id = id;
            RoleName = roleName;
        }
    }
}
