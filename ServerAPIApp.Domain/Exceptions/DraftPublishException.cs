using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions
{
    public class DraftPublishException : ApplicationException
    {
        public DraftPublishException(string msg) : base(msg)
        {

        }
    }
}
