namespace ServerAPIApp.Core.Configs
{
    public class JwtSettings
    {
        public string ValidIssuer { get; set; } = string.Empty;
        public string ValidAudience { get; set; } = string.Empty;
        public int ExpiryInMinutes { get; set; }
    }
}
