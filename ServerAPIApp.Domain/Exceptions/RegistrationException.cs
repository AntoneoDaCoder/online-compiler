using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions
{
    public class RegistrationException : ApplicationException
    {
        public RegistrationException(string msg) : base(msg)
        {

        }
    }
}
