using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions.ConflictExceptions
{
    public class DuplicateException : ConflictException
    {
        public DuplicateException(string msg) : base(msg)
        {

        }
    }
}
