using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions
{
    public class DuplicateException : ApplicationException
    {
        public DuplicateException(string msg) : base(msg)
        {

        }
    }
}
