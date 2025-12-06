using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions.UnauthorizedExceptions
{
    public class RegistrationException : UnauthorizedException
    {
        public RegistrationException(string msg) : base(msg)
        {

        }
    }
}
