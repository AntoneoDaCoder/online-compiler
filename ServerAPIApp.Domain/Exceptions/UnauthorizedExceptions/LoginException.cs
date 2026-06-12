using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions.UnauthorizedExceptions
{
    public class LoginException : UnauthorizedException
    {
        public LoginException(string msg) : base(msg)
        {

        }
    }
}
