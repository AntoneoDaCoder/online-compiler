namespace ServerAPIApp.Domain.Constants
{
    public static class ApplicationConstants
    {
        public const int DeletionRequestReasonMaxLength = 50;
        public const int DeletionRequestReasonMinLength = 10;
        public static readonly TimeSpan GracePeriod = TimeSpan.FromDays(7);
    }
}
