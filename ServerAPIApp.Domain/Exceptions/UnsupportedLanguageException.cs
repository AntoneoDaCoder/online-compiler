using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions
{
    public class UnsupportedLanguageException : ApplicationException
    {
        public UnsupportedLanguageException(string msg) : base(msg)
        {

        }
    }
}
