namespace ServerAPIApp.Domain.Exceptions.BadRequestExceptions
{
    public class InvalidTestTemplateException : BadRequestException
    {
        public InvalidTestTemplateException(string msg) : base(msg)
        {

        }
    }
}
