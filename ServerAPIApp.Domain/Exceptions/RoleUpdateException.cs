using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions
{
    public class RoleUpdateException : ApplicationException
    {
        public RoleUpdateException(string msg) : base(msg)
        {

        }
    }
}
