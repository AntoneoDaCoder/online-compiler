using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions.UnauthorizedExceptions
{
    public class UnauthorizedException : ApplicationException
    {
        public UnauthorizedException(string msg) : base(msg)
        {

        }
    }
}
