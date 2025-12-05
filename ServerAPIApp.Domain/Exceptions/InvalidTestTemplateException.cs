using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions
{
    public class InvalidTestTemplateException : ApplicationException
    {
        public InvalidTestTemplateException(string msg) : base(msg)
        {

        }
    }
}
