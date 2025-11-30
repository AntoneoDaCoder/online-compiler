using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions
{
    public class IncorrectTokenFormatException : ApplicationException
    {
        public IncorrectTokenFormatException(string msg) : base(msg)
        {

        }
    }
}
