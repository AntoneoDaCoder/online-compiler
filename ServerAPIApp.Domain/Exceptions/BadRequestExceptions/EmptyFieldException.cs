namespace ServerAPIApp.Domain.Exceptions.BadRequestExceptions
{
    public class EmptyFieldException : BadRequestException
    {
        public EmptyFieldException(string msg) : base(msg)
        {

        }
    }
}
