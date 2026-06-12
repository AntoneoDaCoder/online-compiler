namespace ServerAPIApp.DAL.Confs
{
    public class MinioConfiguration
    {
        public string BucketName { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public bool UseSsl { get; set; } = false;
    }
}
