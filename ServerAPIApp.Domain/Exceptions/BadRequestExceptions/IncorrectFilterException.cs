using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions.BadRequestExceptions
{
    public class IncorrectFilterException : BadRequestException
    {
        public IncorrectFilterException(string msg) : base(msg)
        {

        }
    }
}
