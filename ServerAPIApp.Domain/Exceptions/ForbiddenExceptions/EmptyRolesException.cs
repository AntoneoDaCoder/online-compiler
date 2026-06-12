using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions.ForbiddenExceptions
{
    public class EmptyRolesException : ForbiddenException
    {
        public EmptyRolesException(string msg) : base(msg) { }
    }
}
