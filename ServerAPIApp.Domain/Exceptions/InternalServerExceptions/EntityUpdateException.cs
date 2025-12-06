using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions.InternalServerExceptions
{
    public class EntityUpdateException : InternalServerException
    {
        public EntityUpdateException(string msg) : base(msg)
        {

        }
    }
}
