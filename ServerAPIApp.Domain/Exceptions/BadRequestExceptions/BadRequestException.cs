namespace ServerAPIApp.Domain.Exceptions.BadRequestExceptions
{
    public class BadRequestException:ApplicationException
    {
        public BadRequestException(string msg):base(msg)
        {
            
        }
    }
}
