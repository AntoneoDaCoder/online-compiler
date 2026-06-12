using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions.UnauthorizedExceptions
{
    public class InvalidTokenException : UnauthorizedException
    {
        public InvalidTokenException(string msg) : base(msg)
        {

        }
    }
}
