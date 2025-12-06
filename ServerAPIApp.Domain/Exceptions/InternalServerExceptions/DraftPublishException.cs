using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions.InternalServerExceptions
{
    public class DraftPublishException : InternalServerException
    {
        public DraftPublishException(string msg) : base(msg)
        {

        }
    }
}
