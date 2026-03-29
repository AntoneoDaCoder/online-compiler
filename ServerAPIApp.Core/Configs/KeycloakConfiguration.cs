namespace ServerAPIApp.Core.Configs
{
    public class KeycloakConfiguration
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string Realm { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public string AdminPassword { get; set; } = string.Empty;
        public string RedirectUrl { get; set; } = string.Empty;
        public int EmailConfirmationLifetimeSeconds { get; set; }
    }
}
