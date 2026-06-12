using System.Text.Json.Serialization;

namespace ServerAPIApp.Contracts.DTOs.Auth
{
    public record ExternalRoleDto
    {
        [JsonPropertyName("id")]
        public string Id { get; } = string.Empty;
        [JsonPropertyName("name")]
        public string RoleName { get; } = string.Empty;

        public ExternalRoleDto(string id, string roleName)
        {
            Id = id;
            RoleName = roleName;
        }
    }
}
