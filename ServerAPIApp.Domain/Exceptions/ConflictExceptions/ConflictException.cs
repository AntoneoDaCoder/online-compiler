using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions.ConflictExceptions
{
    public class ConflictException : ApplicationException
    {
        public ConflictException(string msg) : base(msg)
        {

        }
    }
}
