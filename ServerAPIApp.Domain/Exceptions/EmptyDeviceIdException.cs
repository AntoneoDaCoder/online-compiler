using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions
{
    public class EmptyDeviceIdException : ApplicationException
    {
        public EmptyDeviceIdException(string msg) : base(msg)
        {

        }
    }
}
