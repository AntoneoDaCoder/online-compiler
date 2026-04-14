namespace ServerAPIApp.Core.Configs
{
    public class KeycloakConfiguration
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public string Realm { get; set; } = string.Empty;
        public string FrontEndClientId { get; set; } = string.Empty;
        public string ApiClientId { get; set; } = string.Empty;
        public string ApiClientSecret { get; set; } = string.Empty;
    }
}
