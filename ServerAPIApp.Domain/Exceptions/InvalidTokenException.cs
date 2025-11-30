using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions
{
    public class InvalidTokenException : ApplicationException
    {
        public InvalidTokenException(string msg) : base(msg)
        {

        }
    }
}
