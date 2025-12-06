using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Domain.Exceptions.InternalServerExceptions
{
    public class ObjectStorageUploadException : InternalServerException
    {
        public ObjectStorageUploadException(string msg) : base(msg)
        {

        }
    }
}
