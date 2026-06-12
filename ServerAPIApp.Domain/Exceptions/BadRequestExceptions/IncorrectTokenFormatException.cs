using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions.BadRequestExceptions
{
    public class IncorrectTokenFormatException : BadRequestException
    {
        public IncorrectTokenFormatException(string msg) : base(msg)
        {

        }
    }
}
