using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions.ForbiddenExceptions
{
    public class RestrictedActionException : ForbiddenException
    {
        public RestrictedActionException(string msg) : base(msg)
        {

        }
    }
}
