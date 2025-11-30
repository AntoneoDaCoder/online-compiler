using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions
{
    public class EmptyRolesException : ApplicationException
    {
        public EmptyRolesException(string msg) : base(msg) { }
    }
}
