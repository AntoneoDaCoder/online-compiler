using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions
{
    public class LoginException : ApplicationException
    {
        public LoginException(string msg) : base(msg)
        {

        }
    }
}
