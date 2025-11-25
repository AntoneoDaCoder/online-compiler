using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions
{
    public class ObjectStorageUploadException : ApplicationException
    {
        public ObjectStorageUploadException(string msg) : base(msg)
        {

        }
    }
}
