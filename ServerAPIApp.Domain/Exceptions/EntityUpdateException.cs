using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions
{
    public class EntityUpdateException : ApplicationException
    {
        public EntityUpdateException(string msg) : base(msg)
        {

        }
    }
}
