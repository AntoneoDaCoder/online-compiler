using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions.ForbiddenExceptions
{
    public class ForbiddenException : ApplicationException
    {
        public ForbiddenException(string msg) : base(msg)
        {

        }
    }
}
