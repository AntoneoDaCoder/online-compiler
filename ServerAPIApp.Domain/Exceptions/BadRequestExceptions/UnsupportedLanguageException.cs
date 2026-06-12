using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions.BadRequestExceptions
{
    public class UnsupportedLanguageException : BadRequestException
    {
        public UnsupportedLanguageException(string msg) : base(msg)
        {

        }
    }
}
