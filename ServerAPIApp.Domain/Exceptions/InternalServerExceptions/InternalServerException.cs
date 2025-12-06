using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions.InternalServerExceptions
{
    public class InternalServerException : ApplicationException
    {
        public InternalServerException(string msg) : base(msg)
        {

        }
    }
}
