using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions.UnauthorizedExceptions
{
    public class InvalidRefreshTokenException : UnauthorizedException
    {
        public InvalidRefreshTokenException(string msg) : base(msg)
        {

        }
    }
}
