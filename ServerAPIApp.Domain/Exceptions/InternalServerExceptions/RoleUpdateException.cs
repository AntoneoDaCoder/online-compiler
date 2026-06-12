using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions.InternalServerExceptions
{
    public class RoleUpdateException : InternalServerException
    {
        public RoleUpdateException(string msg) : base(msg)
        {

        }
    }
}
