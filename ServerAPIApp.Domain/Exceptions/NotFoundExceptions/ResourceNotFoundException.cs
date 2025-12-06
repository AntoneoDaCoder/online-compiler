using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions.NotFoundExceptions
{
    public class ResourceNotFoundException : ApplicationException
    {
        public ResourceNotFoundException(string msg) : base(msg)
        {

        }
    }
}
