using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions
{
    public class InvalidRefreshTokenException : ApplicationException
    {
        public InvalidRefreshTokenException(string msg) : base(msg)
        {

        }
    }
}
