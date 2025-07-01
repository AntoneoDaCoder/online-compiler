namespace ServerAPIApp.Core.Enums
{
    public enum RequestStatus
    {
        NoStatus = 0,
        Acknowledged = 1,
        Executing = 2,
        Failed = 3,
        Succeeded = 4,
        Cancelled = 5
    }
}
