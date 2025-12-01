using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions
{
    public class RestrictedActionException : ApplicationException
    {
        public RestrictedActionException(string msg) : base(msg)
        {

        }
    }
}
